using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DenInject.Core {
    public class DiValidator {
        public DiValidator(List<ContainerEntity> entities)
        {
            Dependencies = new Stack<Type>();
            this.entities = entities;
        }

        private Stack<Type> Dependencies { get; set; }

        private List<ContainerEntity> entities { get; set; }

        public void Validate(Type newType)
        {
            if (ContainsCircularDependencies(newType))
                throw new ArgumentException($"Type {newType.ToString()} did contain circular dependencies.");

            //if type doesn't have a constructor, we'll be unable to create it

            if (!HasPublicConstructor(newType))
                throw new ArgumentException($"Type {newType.ToString()} doesn't have a public constructor.");

            var typeConstructors = newType.GetConstructors();


            ParameterInfo[] constructorParams = typeConstructors[0].GetParameters();

            foreach(var param in constructorParams)
            {
                var implementations =
                from entity in entities
                where entity.InterfaceType == param.ParameterType
                select entity.Implementations;

                foreach(List<Implementation> implementation in implementations)
                {
                    Dependencies.Push(newType);
                                                            //if there are many implementations we'll always use first while resolving, 
                                                            //as can be seen in DependencyProvider.cs, so won't check there for other implementations
                    Validate(implementation.First().ImplType);
                    Dependencies.Pop();
                }
            }

        }

        private bool HasPublicConstructor(Type t)
        {
            return t.GetConstructors().Length != 0;
        }

        private bool ContainsCircularDependencies(Type t)
        {
            foreach (var dependency in Dependencies)
            {
                if (dependency == t)
                    return true;
            }

            return false;
        }
    }
}
