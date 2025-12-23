namespace Script.Data
{
    using MessagePack;
    using MessagePack.Formatters;
    using MessagePack.Resolvers;

    /// <summary> MessagePack의 (Cumstom) Attribute 설정에 쓰인다. 없으면 런타임 에러 </summary>
    public static class MessagePackConfig<T>
    {
        public static MessagePackSerializerOptions Options
        {
            get
            {
                return MessagePackSerializerOptions.Standard
                        .WithResolver(CompositeResolver
                        .Create
                        (
                            // 커스텀 포맷터 등록
                            new IMessagePackFormatter[] { new ConcurrentDictionaryFormatter<int, T>() },

                            // 기본 Resolver
                            new IFormatterResolver[] { StandardResolver.Instance }
                        ));
            }
        }
    }
}