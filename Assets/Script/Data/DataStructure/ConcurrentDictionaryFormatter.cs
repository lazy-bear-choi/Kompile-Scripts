namespace Script.Data
{
    using System.Collections.Generic;
    using MessagePack.Formatters;
    using MessagePack;

    /// <summary> MessagePack의 (Cumstom) Attribute 설정에 쓰인다. 없으면 컴파일 에러 </summary>
    public class ConcurrentDictionaryFormatter<TKey, TValue> : IMessagePackFormatter<ConcurrentDictionary<TKey, TValue>>
    {
        public void Serialize(ref MessagePackWriter writer, ConcurrentDictionary<TKey, TValue> value, MessagePackSerializerOptions options)
        {
            options.Resolver.GetFormatterWithVerify<Dictionary<TKey, TValue>>().Serialize(
                ref writer, new Dictionary<TKey, TValue>(value), options);
        }

        public ConcurrentDictionary<TKey, TValue> Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            var dictionary = options.Resolver.GetFormatterWithVerify<Dictionary<TKey, TValue>>()
                                    .Deserialize(ref reader, options);

            return new ConcurrentDictionary<TKey, TValue>(dictionary);
        }
    }
}