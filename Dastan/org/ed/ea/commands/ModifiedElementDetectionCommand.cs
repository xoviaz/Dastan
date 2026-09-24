using System;
using System.Collections.Generic;
using System.Globalization;
using Dastan.org.ed.ea.constants;
using Dastan.org.ed.ea.entity;
using Dastan.org.ed.ea.repository;
using Dastan.org.ed.ea.services.modifications;
using Dastan.org.ed.ea.util;
using EA;
using Attribute = EA.Attribute;

namespace Dastan.org.ed.ea.commands
{
    public class ModifiedElementDetectionCommand : ICommand
    {
        private class ElementSnapshot
        {
            public string Name;
            public string Alias;
            public string Notes;
            public string Stereotype;
            public Dictionary<string, string> TaggedValues;

            // Attributes and operations, keyed by their own GUID rather than their name,
            // so renaming one reads as a rename instead of one thing vanishing and
            // another appearing.
            public Dictionary<string, MemberSnapshot> Attributes;
            public Dictionary<string, MemberSnapshot> Operations;
        }

        // An attribute or an operation. They carry different EA types and share no
        // interface, so what they have in common is copied out once and compared here.
        private class MemberSnapshot
        {
            public string Name;
            public string Alias;
            public string Notes;
            public string Stereotype;
            public string Type;
            public string Default;
            public Dictionary<string, string> TaggedValues;
        }

        private readonly ElementRepository _elementRepository = new ElementRepository();
        private readonly Dictionary<string, ElementSnapshot> _snapshots = new Dictionary<string, ElementSnapshot>();
        private readonly List<string> _allowedProfiles = new List<string>() { Profiles.PlmSchema, Profiles.Widget };

        private readonly List<IModificationObserver> _observers = new List<IModificationObserver>
        {
            new ModificationLogObserver()
        };

        // The element the selection is sitting on, so it can be checked for changes once
        // the selection moves off it.
        private string _previous;

        // Nesting depth of Suspend/Resume rather than a flag, so overlapping callers
        // cannot switch tracking back on while another is still writing.
        private int _suspended;

        // Stops tracking from recording the edits that undoing a change makes. Without it
        // a revert is itself logged as a change, and a delta script then carries both the
        // original edit and its undo -- which cancel out, at twice the length.
        public void Suspend()
        {
            _suspended++;
        }

        // Resumes, and re-reads the element so the next comparison starts from what the
        // revert actually left behind rather than from a snapshot taken before it.
        public void Resume(Element element)
        {
            if (_suspended > 0) _suspended--;
            if (element == null || _suspended > 0) return;

            _snapshots[element.ElementGUID] = Snapshot(element);
        }

        public void Subscribe(IModificationObserver observer)
        {
            _observers.Add(observer);
        }

        public void OnContextItemChanged(Repository repository, string guid, ObjectType ot)
        {
            if (_suspended > 0) return;

            // Compare what is being left before remembering what is being arrived at.
            //
            // EA does not announce every edit -- a tagged value changed on an attribute is
            // one it can stay quiet about -- and a selection change is the next moment
            // anything here is known to run. This used to overwrite the snapshot straight
            // away, so an unannounced change was compared against nothing and disappeared.
            DetectPrevious(repository);

            Element el = ResolveElement(repository, guid, ot);
            if (el == null) return;
            if (!IsInAllowedProfile(el)) return;

            _previous = el.ElementGUID;

            // Only when nothing is held yet. An existing snapshot may be carrying a change
            // that could not be delivered -- the panel was closed, say -- and replacing it
            // would throw that change away.
            if (!_snapshots.ContainsKey(_previous))
                _snapshots[_previous] = Snapshot(el);
        }

        private void DetectPrevious(Repository repository)
        {
            if (string.IsNullOrEmpty(_previous)) return;

            Element el = null;
            try
            {
                el = repository.GetElementByGuid(_previous);
            }
            catch
            {
                // Deleted while it was the selected item.
            }

            if (el == null)
            {
                _previous = null;
                return;
            }

            Detect(new Context(repository, ObjectType.otElement, _previous), el);
        }

        public void Execute(Context context)
        {
            if (_suspended > 0) return;

            // A change to an attribute or an operation arrives under that member's own
            // GUID, but everything is snapshotted per element, so the owner is what has to
            // be found before anything can be compared.
            Element el = ResolveElement(context.Repository, context.Guid, context.Type);
            if (el == null) return;

            Detect(context, el);
        }

        private void Detect(Context context, Element el)
        {
            string guid = el.ElementGUID;
            if (!_snapshots.TryGetValue(guid, out ElementSnapshot oldState)) return;

            if (!IsInAllowedProfile(el)) return;

            string date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

            bool delivered = true;
            try
            {
                var changes = new List<ModificationEvent>();

                if (el.Name != oldState.Name)
                {
                    changes.Add(new ModificationEvent(el, date, el.Name, el.Type, el.Stereotype, "name", oldState.Name, el.Name));
                }

                if (el.Alias != oldState.Alias)
                {
                    changes.Add(new ModificationEvent(
                        el, date, el.Name, el.Type, el.Stereotype,
                        "alias", oldState.Alias, el.Alias));
                }

                if (el.Notes != oldState.Notes)
                {
                    changes.Add(new ModificationEvent(
                        el, date, el.Name, el.Type, el.Stereotype,
                        "notes", oldState.Notes, el.Notes));
                }

                if (el.Stereotype != oldState.Stereotype)
                {
                    changes.Add(new ModificationEvent(
                        el, date, el.Name, el.Type, el.Stereotype,
                        "stereotype", oldState.Stereotype, el.Stereotype));
                }

                // Tagged values
                foreach (TaggedValue tv in el.TaggedValues)
                {
                    string newVal = tv.Value ?? "";

                    if (oldState.TaggedValues.TryGetValue(tv.Name, out var oldVal))
                    {
                        if (oldVal != newVal)
                        {
                            changes.Add(new ModificationEvent(
                                el, date, el.Name, el.Type, el.Stereotype,
                                tv.Name, oldVal, newVal));
                        }
                    }
                    else
                    {
                        changes.Add(new ModificationEvent(
                            el, date, el.Name, el.Type, el.Stereotype,
                            tv.Name, null, newVal));
                    }
                }

                DiffMembers(el, date, oldState.Attributes, SnapshotAttributes(el),
                    ModificationProperties.AttributeAdded, ModificationProperties.AttributeRemoved, changes);

                DiffMembers(el, date, oldState.Operations, SnapshotOperations(el),
                    ModificationProperties.OperationAdded, ModificationProperties.OperationRemoved, changes);

                delivered = Notify(context, changes);
            }
            catch (Exception ex)
            {
                LogTrackingError(context, ex);
            }
            finally
            {
                if (delivered)
                    _snapshots[guid] = Snapshot(el);
            }

        }

        // A member that appeared or disappeared is reported against the ELEMENT, because
        // that is the object MQL modifies. A member that merely changed is reported
        // against itself, because "modify attribute X description ..." names the
        // attribute, so the attribute's name and stereotype are what the row has to carry.
        private static void DiffMembers(Element el, string date,
            Dictionary<string, MemberSnapshot> before, Dictionary<string, MemberSnapshot> after,
            string addedProperty, string removedProperty, List<ModificationEvent> changes)
        {
            foreach (KeyValuePair<string, MemberSnapshot> pair in after)
            {
                if (!before.TryGetValue(pair.Key, out MemberSnapshot old))
                {
                    changes.Add(new ModificationEvent(el, date, el.Name, el.Type, el.Stereotype,
                        addedProperty, "", pair.Value.Name));
                    continue;
                }

                DiffMember(el, date, old, pair.Value, changes);
            }

            foreach (KeyValuePair<string, MemberSnapshot> pair in before)
            {
                if (after.ContainsKey(pair.Key)) continue;

                changes.Add(new ModificationEvent(el, date, el.Name, el.Type, el.Stereotype,
                    removedProperty, pair.Value.Name, ""));
            }
        }

        private static void DiffMember(Element el, string date, MemberSnapshot old, MemberSnapshot now,
            List<ModificationEvent> changes)
        {
            AddMemberChange(changes, el, date, now, "name", old.Name, now.Name);
            AddMemberChange(changes, el, date, now, "alias", old.Alias, now.Alias);
            AddMemberChange(changes, el, date, now, "description", old.Notes, now.Notes);
            AddMemberChange(changes, el, date, now, "stereotype", old.Stereotype, now.Stereotype);
            AddMemberChange(changes, el, date, now, "type", old.Type, now.Type);
            AddMemberChange(changes, el, date, now, "default", old.Default, now.Default);

            foreach (KeyValuePair<string, string> tag in now.TaggedValues)
            {
                old.TaggedValues.TryGetValue(tag.Key, out string oldValue);
                AddMemberChange(changes, el, date, now, tag.Key, oldValue, tag.Value);
            }
        }

        // The element still rides along as the event's Element, so the log row can be
        // walked back to something EA can select and the profile check has something to
        // work with. Only the displayed name, type and stereotype are the member's.
        private static void AddMemberChange(List<ModificationEvent> changes, Element el, string date,
            MemberSnapshot member, string property, string oldValue, string newValue)
        {
            if ((oldValue ?? "") == (newValue ?? "")) return;

            changes.Add(new ModificationEvent(el, date, member.Name, member.Type, member.Stereotype,
                property, oldValue, newValue));
        }

        private static void LogTrackingError(Context context, Exception ex)
        {
            try
            {
                TabUtility.Create(context).LogTab.Log("[Modification Tracking Error] " + ex, 0);
            }
            catch
            {
                // Best-effort diagnostic only.
            }
        }

        public void OnConnectorCreated(Repository repository, EventProperties info)
        {
            if (_suspended > 0) return;

            try
            {
                Connector conn = GetConnectorFromEvent(repository, info);
                if (conn == null) return;
                LogConnectorChange(repository, conn, ModificationProperties.Connection, isNew: true);
            }
            catch (Exception ex)
            {
                LogTrackingError(new Context(repository), ex);
            }
        }

        public void OnConnectorDeleted(Repository repository, EventProperties info)
        {
            if (_suspended > 0) return;

            try
            {
                Connector conn = GetConnectorFromEvent(repository, info);
                if (conn == null) return;
                LogConnectorChange(repository, conn, ModificationProperties.Disconnection, isNew: false);
            }
            catch (Exception ex)
            {
                LogTrackingError(new Context(repository), ex);
            }
        }

        // EA announces a member about to be deleted while it still exists, which is the
        // only moment its name and its owner can still be read.
        //
        // Comparing snapshots cannot do this job. The notification that follows a deletion
        // arrives under the deleted member's own GUID, and by then EA no longer knows that
        // GUID, so there is nothing left to resolve it back to an element and the whole
        // comparison is skipped. It is the same reason connectors have always been caught
        // this way rather than by diffing.
        public void OnAttributeDeleted(Repository repository, EventProperties info)
        {
            if (_suspended > 0) return;

            try
            {
                int id = IdFromEvent(info);
                if (id <= 0) return;

                Attribute attribute = repository.GetAttributeByID(id);
                if (attribute == null) return;

                LogMemberRemoved(repository, _elementRepository.SafeGetElementById(repository, attribute.ParentID),
                    attribute.Name, attribute.AttributeGUID, ModificationProperties.AttributeRemoved);
            }
            catch (Exception ex)
            {
                LogTrackingError(new Context(repository), ex);
            }
        }

        public void OnOperationDeleted(Repository repository, EventProperties info)
        {
            if (_suspended > 0) return;

            try
            {
                int id = IdFromEvent(info);
                if (id <= 0) return;

                Method method = repository.GetMethodByID(id);
                if (method == null) return;

                LogMemberRemoved(repository, _elementRepository.SafeGetElementById(repository, method.ParentID),
                    method.Name, method.MethodGUID, ModificationProperties.OperationRemoved);
            }
            catch (Exception ex)
            {
                LogTrackingError(new Context(repository), ex);
            }
        }

        private void LogMemberRemoved(Repository repository, Element element, string memberName, string memberGuid,
            string property)
        {
            if (element == null || string.IsNullOrEmpty(memberName)) return;
            if (!IsInAllowedProfile(element)) return;

            string date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

            // Reported against the element, because that is the object MQL modifies:
            // "modify type X remove attribute Y".
            Notify(new Context(repository), new List<ModificationEvent>
            {
                new ModificationEvent(element, date, element.Name, element.Type, element.Stereotype,
                    property, memberName, "")
            });

            // This runs before the deletion, so a snapshot taken now still contains the
            // member. Dropping it by hand is what stops the next comparison reporting the
            // same removal a second time.
            ElementSnapshot snapshot = Snapshot(element);
            snapshot.Attributes.Remove(memberGuid);
            snapshot.Operations.Remove(memberGuid);
            _snapshots[element.ElementGUID] = snapshot;
        }

        public bool HasAccess(Repository repository)
        {
            return true;
        }

        // Elements are addressed by their own GUID; an attribute or an operation is
        // addressed by the element that owns it.
        private Element ResolveElement(Repository repository, string guid, ObjectType ot)
        {
            if (repository == null || string.IsNullOrEmpty(guid)) return null;

            try
            {
                switch (ot)
                {
                    case ObjectType.otElement:
                        return repository.GetElementByGuid(guid);

                    case ObjectType.otAttribute:
                        Attribute attribute = repository.GetAttributeByGuid(guid);
                        return attribute == null
                            ? null
                            : _elementRepository.SafeGetElementById(repository, attribute.ParentID);

                    case ObjectType.otMethod:
                        Method method = repository.GetMethodByGuid(guid);
                        return method == null
                            ? null
                            : _elementRepository.SafeGetElementById(repository, method.ParentID);

                    default:
                        return null;
                }
            }
            catch
            {
                // A GUID EA no longer knows, most often because the thing was just deleted.
                return null;
            }
        }

        private bool IsInAllowedProfile(Element el)
        {
            if (el == null) return false;
            string fq = el.FQStereotype ?? "";
            int sep = fq.IndexOf("::", StringComparison.Ordinal);
            if (sep <= 0) return false;

            return _allowedProfiles.Contains(fq.Substring(0, sep));
        }

        private static Connector GetConnectorFromEvent(Repository repository, EventProperties info)
        {
            int id = IdFromEvent(info);
            return id <= 0 ? null : repository.GetConnectorByID(id);
        }

        // Every one of EA's delete and create broadcasts leads with the id of the thing it
        // is about.
        private static int IdFromEvent(EventProperties info)
        {
            if (info == null || info.Count == 0) return 0;

            return int.TryParse(Convert.ToString(info.Get(0).Value), out int id) ? id : 0;
        }

        private void LogConnectorChange(Repository repository, Connector connector, string property, bool isNew)
        {
            Element client = _elementRepository.SafeGetElementById(repository, connector.ClientID);
            Element supplier = _elementRepository.SafeGetElementById(repository, connector.SupplierID);
            Element anchor = IsInAllowedProfile(client) ? client : (IsInAllowedProfile(supplier) ? supplier : null);
            if (anchor == null) return;
            Element other = ReferenceEquals(anchor, client) ? supplier : client;
            string description = $"{connector.Stereotype} » {other?.Stereotype ?? ""} » {other?.Name ?? ""}";

            string date = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            Context context = new Context(repository);

            ModificationEvent change = isNew ? new ModificationEvent(anchor, date, anchor.Name, anchor.Type, anchor.Stereotype, property, "", description)
                : new ModificationEvent(anchor, date, anchor.Name, anchor.Type, anchor.Stereotype, property, description, "");

            Notify(context, new List<ModificationEvent> { change });
        }

        private bool Notify(Context context, IReadOnlyList<ModificationEvent> changes)
        {
            bool delivered = true;
            foreach (IModificationObserver observer in _observers)
                delivered &= observer.OnModified(context, changes);

            return delivered;
        }

        private static ElementSnapshot Snapshot(Element el)
        {
            return new ElementSnapshot
            {
                Name = el.Name,
                Alias = el.Alias,
                Notes = el.Notes,
                Stereotype = el.Stereotype,
                TaggedValues = TagMap(el.TaggedValues),
                Attributes = SnapshotAttributes(el),
                Operations = SnapshotOperations(el)
            };
        }

        private static Dictionary<string, MemberSnapshot> SnapshotAttributes(Element el)
        {
            var members = new Dictionary<string, MemberSnapshot>();

            Collection attributes = el.Attributes;
            Refresh(attributes);

            foreach (Attribute attribute in attributes)
            {
                members[attribute.AttributeGUID] = new MemberSnapshot
                {
                    Name = attribute.Name,
                    Alias = attribute.Alias,
                    Notes = attribute.Notes,
                    Stereotype = attribute.Stereotype,
                    Type = attribute.Type,
                    Default = attribute.Default,
                    TaggedValues = TagMap(attribute.TaggedValues)
                };
            }

            return members;
        }

        private static Dictionary<string, MemberSnapshot> SnapshotOperations(Element el)
        {
            var members = new Dictionary<string, MemberSnapshot>();

            Collection methods = el.Methods;
            Refresh(methods);

            foreach (Method method in methods)
            {
                members[method.MethodGUID] = new MemberSnapshot
                {
                    Name = method.Name,
                    Alias = method.Alias,
                    Notes = method.Notes,
                    Stereotype = method.Stereotype,
                    // An operation has no data type of its own; what it returns is the
                    // nearest thing, and it is what changes when the signature does.
                    Type = method.ReturnType,
                    Default = "",
                    TaggedValues = TagMap(method.TaggedValues)
                };
            }

            return members;
        }

        // EA hands back collections it has already loaded. An attribute's tagged values
        // read as they were when the element was first touched, so a value edited since
        // compares equal to itself and the diff concludes nothing happened -- until
        // something else drops EA's cache and the change suddenly appears. Re-reading
        // first is what makes the comparison about the model rather than about the cache.
        private static void Refresh(Collection collection)
        {
            try
            {
                collection.Refresh();
            }
            catch
            {
                // Not every collection supports it, and a stale read beats no tracking.
            }
        }

        // Elements, attributes and operations each carry their own tag type and share no
        // interface between them, so each is asked in turn rather than three times over.
        private static Dictionary<string, string> TagMap(Collection tags)
        {
            var map = new Dictionary<string, string>();
            if (tags == null) return map;

            Refresh(tags);

            foreach (object tag in tags)
            {
                AttributeTag attributeTag = tag as AttributeTag;
                if (attributeTag != null)
                {
                    map[attributeTag.Name] = attributeTag.Value ?? "";
                    continue;
                }

                MethodTag methodTag = tag as MethodTag;
                if (methodTag != null)
                {
                    map[methodTag.Name] = methodTag.Value ?? "";
                    continue;
                }

                TaggedValue taggedValue = tag as TaggedValue;
                if (taggedValue != null) map[taggedValue.Name] = taggedValue.Value ?? "";
            }

            return map;
        }
    }
}
