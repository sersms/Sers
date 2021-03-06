﻿using Sers.Core.Util.Threading;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Test.Sers.Mq.Client.Exe
{
    public class LongTaskHelpTest
    {
        public static void Test()
        {

            var task = new LongTaskHelp {action =()=>{

                while (true)
                {
                    Console.WriteLine("run in thread");
                    Thread.Sleep(1000);
                }

            } };
            Console.WriteLine("task.Start");
            task.Start();
            Task.Run(()=> {
                Thread.Sleep(3000);

                Console.WriteLine("task.Stop");
                task.Stop();

                Thread.Sleep(3000);
                Console.WriteLine("task.Start");
                task.Start();
            });


            while (true)
            {
                Console.WriteLine("run in Main");
                Thread.Sleep(1000);
            }
        }
    }
}
