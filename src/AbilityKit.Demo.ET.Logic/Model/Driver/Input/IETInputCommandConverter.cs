using System;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.Host.Framework;

namespace ET.Logic
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class ETInputCommandConverterAttribute : Attribute
    {
        public Type CommandType { get; }

        public ETInputCommandConverterAttribute(Type commandType)
        {
            CommandType = commandType ?? throw new ArgumentNullException(nameof(commandType));
        }
    }

    public interface IETInputCommandConverter
    {
        Type CommandType { get; }
        bool TryConvert(object command, FrameIndex frameIndex, out PlayerInputCommand playerCommand);
    }
}
