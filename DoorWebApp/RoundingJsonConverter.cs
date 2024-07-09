using Newtonsoft.Json;

namespace DoorWebApp
{
    /// <summary>
    /// 設定浮點數轉換的位數(JSON轉換時)
    /// htDoor://blog.yowko.com/decimal-double-float-json-format/
    /// </summary>
    public class RoundingJsonConverter : JsonConverter
    {
        //指定精準度
        int _precision;
        //指定四捨五入的傾向
        MidpointRounding _rounding;

        //預設精準度小數點下 4 位
        public RoundingJsonConverter() : this(4)
        {
        }

        public RoundingJsonConverter(int precision)
        : this(precision, MidpointRounding.AwayFromZero)
        {
        }

        public RoundingJsonConverter(int precision, MidpointRounding rounding)
        {
            _precision = precision;
            _rounding = rounding;
        }

        public override bool CanRead
        {
            get { return false; }
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(decimal);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            decimal _value = (decimal)value;
            //為值補 0
            writer.WriteValue(decimal.Parse(Math.Round(_value, _precision, _rounding).ToString("0.".PadRight(2 + _precision, '0'))));
        }
    }
}
