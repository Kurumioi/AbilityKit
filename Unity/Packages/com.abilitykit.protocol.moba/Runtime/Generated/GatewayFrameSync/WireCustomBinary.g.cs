using System;
using MemoryPack;

namespace AbilityKit.Protocol.Moba.Generated.GatewayFrameSync
{
    public static class WireCustomBinary
    {
        public static ArraySegment<byte> Serialize(in WireSubmitFrameInputReq req)
        {
            return ToSegment(MemoryPackSerializer.Serialize(req));
        }

        public static ArraySegment<byte> Serialize(in WireSubmitFrameInputRes res)
        {
            return ToSegment(MemoryPackSerializer.Serialize(res));
        }

        public static ArraySegment<byte> Serialize(in WireFramePushedPush push)
        {
            return ToSegment(MemoryPackSerializer.Serialize(push));
        }

        public static WireSubmitFrameInputReq DeserializeSubmitFrameInputReq(ArraySegment<byte> payload)
        {
            return DeserializeSubmitFrameInputReq(ToSpan(payload));
        }

        public static WireSubmitFrameInputReq DeserializeSubmitFrameInputReq(ReadOnlyMemory<byte> payload)
        {
            return DeserializeSubmitFrameInputReq(payload.Span);
        }

        public static WireSubmitFrameInputReq DeserializeSubmitFrameInputReq(ReadOnlySpan<byte> payload)
        {
            return payload.Length == 0
                ? default
                : MemoryPackSerializer.Deserialize<WireSubmitFrameInputReq>(payload);
        }

        public static WireSubmitFrameInputRes DeserializeSubmitFrameInputRes(ArraySegment<byte> payload)
        {
            return DeserializeSubmitFrameInputRes(ToSpan(payload));
        }

        public static WireSubmitFrameInputRes DeserializeSubmitFrameInputRes(ReadOnlyMemory<byte> payload)
        {
            return DeserializeSubmitFrameInputRes(payload.Span);
        }

        public static WireSubmitFrameInputRes DeserializeSubmitFrameInputRes(ReadOnlySpan<byte> payload)
        {
            return payload.Length == 0
                ? default
                : MemoryPackSerializer.Deserialize<WireSubmitFrameInputRes>(payload);
        }

        public static WireFramePushedPush DeserializeFramePushedPush(ArraySegment<byte> payload)
        {
            return DeserializeFramePushedPush(ToSpan(payload));
        }

        public static WireFramePushedPush DeserializeFramePushedPush(ReadOnlyMemory<byte> payload)
        {
            return DeserializeFramePushedPush(payload.Span);
        }

        public static WireFramePushedPush DeserializeFramePushedPush(ReadOnlySpan<byte> payload)
        {
            return payload.Length == 0
                ? default
                : MemoryPackSerializer.Deserialize<WireFramePushedPush>(payload);
        }

        private static ArraySegment<byte> ToSegment(byte[] bytes)
        {
            return new ArraySegment<byte>(bytes ?? Array.Empty<byte>());
        }

        private static ReadOnlySpan<byte> ToSpan(ArraySegment<byte> payload)
        {
            return payload.Array == null
                ? ReadOnlySpan<byte>.Empty
                : new ReadOnlySpan<byte>(payload.Array, payload.Offset, payload.Count);
        }
    }
}
