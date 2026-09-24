using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Dastan.org.ed.ea.di
{
    public class ServiceContainer
    {
        private enum Lifetime
        {
            Singleton,
            Transient,
        }

        private class Registration
        {
            public Lifetime Lifetime;
            public Func<ServiceContainer, object> Factory;
            public object SingletonInstance;
        }
        
        private readonly Dictionary<Type, Registration> _registrations = new Dictionary<Type, Registration>();

        public void RegisterSingleton<TImplementation>() where TImplementation : class
        {
            _registrations[typeof(TImplementation)] = new Registration()
            {
                Lifetime = Lifetime.Singleton,
                Factory = c => c.CreateInstance(typeof(TImplementation))
            };
        }

        public void RegisterSingleton<TService, TImplementation>() where TImplementation : class, TService
        {
            _registrations[typeof(TService)] = new Registration
            {
                Lifetime = Lifetime.Singleton,
                Factory = c => c.CreateInstance(typeof(TImplementation))
            };
        }

        public void RegisterTransient<TService, TImplementation>() where TImplementation : class, TService
        {
            _registrations[typeof(TService)] = new Registration
            {
                Lifetime = Lifetime.Transient,
                Factory = c => c.CreateInstance(typeof(TImplementation))
            };
        }

        public void RegisterInstance<TService>(TService instance)
        {
            _registrations[typeof(TService)] = new Registration
            {
                Lifetime = Lifetime.Singleton,
                SingletonInstance = instance
            };
        }

        public T Resolve<T>()
        {
            return (T) Resolve(typeof(T));
        }

        public object Resolve(Type type)
        {
            if (_registrations.TryGetValue(type, out Registration registration))
            {
                if (registration.Lifetime == Lifetime.Singleton)
                {
                    if (registration.SingletonInstance == null)
                        registration.SingletonInstance = registration.Factory(this);
                    return registration.SingletonInstance;
                }

                return registration.Factory(this);
            }
            
            return CreateInstance(type);
        }

        private object CreateInstance(Type type)
        {
            ConstructorInfo ctor = type.GetConstructors()
                .OrderByDescending(c => c.GetParameters().Length)
                .FirstOrDefault();

            if (ctor == null)
                throw new InvalidOperationException($"No public constructor found for {type.FullName}");

            ParameterInfo[] parameters = ctor.GetParameters();
            object[] args = new object[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
                args[i] = Resolve(parameters[i].ParameterType);
            
            return Activator.CreateInstance(type, args);
        }
    }
}