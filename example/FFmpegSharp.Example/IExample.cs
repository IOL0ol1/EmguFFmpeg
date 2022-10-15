using System.Collections.Generic;

namespace FFmpegSharp.AppTest
{
    public interface IExample
    {
        void Execute();
    }

    public abstract class ExampleBase : IExample
    {
        protected Dictionary<string, object> parames = new Dictionary<string, object>();

        protected T GetParame<T>(string key)=> (T)parames[key];

        public abstract void Execute();
    }
}
