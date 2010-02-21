using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using Autofac.Builder;
using Autofac.Modules;
using NServiceBus.ObjectBuilder.Autofac.Internal;

namespace NServiceBus.ObjectBuilder.Autofac
{
    ///<summary>
    /// Autofac implementation of IContainer.
    ///</summary>
    internal class AutofacObjectBuilder : Common.IContainer
    {
        /// <summary>
        /// The container itself.
        /// </summary>
        private readonly IContainer container;

        ///<summary>
        /// Instantiates the class saving the given container.
        ///</summary>
        ///<param name="container"></param>
        public AutofacObjectBuilder(IContainer container)
        {
            this.container = container;
            Initialize();
        }

        ///<summary>
        /// Instantites the class with a new Autofac container.
        ///</summary>
        public AutofacObjectBuilder() : this(new Container())
        {
        }

        
        ///<summary>
        /// Build an instance of a given type using Autofac.
        ///</summary>
        ///<param name="typeToBuild"></param>
        ///<returns></returns>
        public object Build(Type typeToBuild)
        {
            return container.Resolve(typeToBuild);
        }

        ///<summary>
        /// Build all instances of a given type using Autofac.
        ///</summary>
        ///<param name="typeToBuild"></param>
        ///<returns></returns>
        public IEnumerable<object> BuildAll(Type typeToBuild)
        {
            return container.ResolveAll(typeToBuild);
        }

        void Common.IContainer.Configure(Type component, ComponentCallModelEnum callModel)
        {
            IComponentRegistration registration = GetComponentRegistration(component);

            if (registration == null)
            {
                var builder = new ContainerBuilder();
                InstanceScope instanceScope = GetInstanceScopeFrom(callModel);
                Type[] services = component.GetAllServices().ToArray();
                builder.Register(component).As(services).WithScope(instanceScope).OnActivating(ActivatingHandler.InjectUnsetProperties);
                builder.Build(container);
            }
        }

        ///<summary>
        /// Configure the value of a named component property.
        ///</summary>
        ///<param name="component"></param>
        ///<param name="property"></param>
        ///<param name="value"></param>
        public void ConfigureProperty(Type component, string property, object value)
        {
            var registration = GetComponentRegistration(component);

            if (registration == null)
            {
                return;
            }

            registration.Activating += (sender, e) => e.Instance.SetPropertyValue(property, value);
        }

        ///<summary>
        /// Register a singleton instance of a dependency within Autofac.
        ///</summary>
        ///<param name="lookupType"></param>
        ///<param name="instance"></param>
        public void RegisterSingleton(Type lookupType, object instance)
        {
            var builder = new ContainerBuilder();
            builder.Register(instance).As(lookupType).ExternallyOwned().OnActivating(ActivatingHandler.InjectUnsetProperties);
            builder.Build(container);
        }

        public bool HasComponent(Type componentType)
        {
            return container.IsRegistered(componentType);
        }

        private void Initialize()
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule(new ImplicitCollectionSupportModule());
            builder.Build(container);
        }

        private static InstanceScope GetInstanceScopeFrom(ComponentCallModelEnum callModel)
        {
            switch (callModel)
            {
                case ComponentCallModelEnum.Singlecall:
                    return InstanceScope.Factory;
                case ComponentCallModelEnum.Singleton:
                    return InstanceScope.Singleton;
            }

            return InstanceScope.Container;
        }

        private IComponentRegistration GetComponentRegistration(Type concreteComponent)
        {
            return container.GetAllRegistrations().Where(x => x.Descriptor.BestKnownImplementationType == concreteComponent).FirstOrDefault();
        }
    }
}


