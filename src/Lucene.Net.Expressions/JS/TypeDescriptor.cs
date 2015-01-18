using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace Lucene.Net.Expressions.JS
{
    class TypeDescriptor:Descriptor
    {
        internal TypeDescriptor(Type type)
            : this(type, type == null ? null : type.Assembly, true) { }

        internal TypeDescriptor(Type type, bool showKind)
            : base()
        {
            this.CreateDescriptor(type, type.Assembly, showKind);
        }

        internal TypeDescriptor(Type type, Assembly containingAssembly)
            : this(type, containingAssembly, true) { }

        internal TypeDescriptor(Type type, Assembly containingAssembly, bool showKind)
            : base()
        {
            this.CreateDescriptor(type, containingAssembly, showKind);
        }

        private static void CheckAssembly(Assembly containingAssembly)
        {
            if (containingAssembly == null)
            {
                throw new ArgumentNullException("containingAssembly");
            }
        }

        private void CreateDescriptor(Type type, Assembly containingAssembly, bool showKind)
        {
            var builder = new StringBuilder();

            if (type == null || type == typeof(void))
            {
                builder.Append("void");
            }
            else
            {
                TypeDescriptor.CheckAssembly(containingAssembly);
                var elementType = type.GetRootElementType();

                if (TypeDescriptor.IsSpecial(type))
                {
                    builder.Append(GetSpecialName(type));
                }
                else
                {
                    if (type.Assembly.Equals(containingAssembly))
                    {
                        builder.Append(TypeDescriptor.GetTypeKind(type, showKind))
                            .Append(" ")
                            .Append(TypeDescriptor.GetTypeName(type));
                    }
                    else
                    {
                        builder.Append(TypeDescriptor.GetTypeKind(type, showKind))
                            .Append(" [")
                            .Append(type.Assembly.GetName().Name).Append("]")
                            .Append(TypeDescriptor.GetTypeName(type));
                    }

                    TypeDescriptor.AddGenerics(type, containingAssembly, builder);

                    builder.Append(type.Name.Replace(elementType.Name, string.Empty));
                }
            }

            this.Value = builder.ToString().Trim();
        }

        private static void AddGenerics(Type type, Assembly containingAssembly, StringBuilder builder)
        {
            var genericArguments = type.GetGenericArguments();

            if (genericArguments != null && genericArguments.Length > 0)
            {
                var genericCount = "`" +
                    genericArguments.Length.ToString(CultureInfo.CurrentCulture);

                if (!builder.ToString().EndsWith(genericCount, StringComparison.CurrentCulture))
                {
                    builder.Append(genericCount);
                }

                builder.Append("<");
                var genericTypes = new List<string>();

                foreach (var genericArgument in genericArguments)
                {
                    if (genericArgument.IsGenericParameter)
                    {
                        genericTypes.Add(genericArgument.Name);
                    }
                    else
                    {
                        genericTypes.Add(new TypeDescriptor(
                            genericArgument.GetRootElementType(), containingAssembly).Value);
                    }
                }

                builder.Append(string.Join(", ", genericTypes.ToArray()));
                builder.Append(">");
            }
        }

        private static string GetSpecialName(Type type)
        {
            var specialName = string.Empty;

            var elementType = type.GetRootElementType();

            if (elementType.Equals(typeof(float)))
            {
                specialName = "float32";
            }
            else if (elementType.Equals(typeof(double)))
            {
                specialName = "float64";
            }
            else if (elementType.Equals(typeof(long)))
            {
                specialName = "int64";
            }
            else if (elementType.Equals(typeof(ulong)))
            {
                specialName = "uint64";
            }
            else if (elementType.Equals(typeof(int)))
            {
                specialName = "int32";
            }
            else if (elementType.Equals(typeof(uint)))
            {
                specialName = "uint32";
            }
            else if (elementType.Equals(typeof(short)))
            {
                specialName = "int16";
            }
            else if (elementType.Equals(typeof(ushort)))
            {
                specialName = "uint16";
            }
            else if (elementType.Equals(typeof(sbyte)))
            {
                specialName = "int8";
            }
            else if (elementType.Equals(typeof(byte)))
            {
                specialName = "uint8";
            }
            else if (elementType.Equals(typeof(string)))
            {
                specialName = "string";
            }
            else if (elementType.Equals(typeof(object)))
            {
                specialName = "object";
            }

            return specialName + type.Name.Replace(elementType.Name, string.Empty);
        }

        private static string GetTypeKind(Type type, bool showKind)
        {
            var kind = string.Empty;

            if (showKind)
            {
                if (type.GetRootElementType().IsValueType)
                {
                    kind = "valuetype";
                }
                else
                {
                    kind = "class";
                }
            }

            return kind;
        }

        private static string GetTypeName(Type type)
        {
            var elementType = type.GetRootElementType();
            return elementType.Namespace + "." + elementType.Name;
        }

        private static bool IsSpecial(Type type)
        {
            var elementType = type.GetRootElementType();

            return elementType.Equals(typeof(float)) | elementType.Equals(typeof(double)) |
                elementType.Equals(typeof(long)) | elementType.Equals(typeof(int)) |
                elementType.Equals(typeof(ulong)) | elementType.Equals(typeof(uint)) |
                elementType.Equals(typeof(short)) | elementType.Equals(typeof(ushort)) |
                elementType.Equals(typeof(byte)) | elementType.Equals(typeof(sbyte)) |
                elementType.Equals(typeof(void)) | elementType.Equals(typeof(string)) |
                elementType.Equals(typeof(object));
        }    
    }
}