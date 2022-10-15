using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFmpegSharp.AppTest
{
    internal class MultiThread : ExampleBase
    {
        public MultiThread(string inputFile,string outputFile)
        {
            parames["inputFile"] = inputFile;
            parames["outputFile"] = outputFile;
        }


        public override void Execute()
        {
            var input = GetParame<string>("inputFile");
            var output = GetParame<string>("outputFile");





        }
    }
}
