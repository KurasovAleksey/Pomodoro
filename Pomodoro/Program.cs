using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Timers;
using System.IO;
using System.Resources;
using System.Reflection;

namespace Pomodoro
{
    class Program
    {
        static object blocker = new object();

        static volatile bool isClocking = false;
        static volatile bool isPause = false;
        static volatile bool isExit = false;
        static volatile bool isGoingToExit = false;
        static volatile bool isReadPrinciples = false;
        static volatile bool isWorkTime = true;

        static Thread listener = new Thread(Listener);
        static Thread breaker = new Thread(Breaker);
        static DateTime time = new DateTime();
        static volatile System.Timers.Timer timer;

        static string strGreeting;
        static string strClockingGo;
        static string strClockingPause;
        static string strPrinciples;
        static string strGointToExit;

        static int workTimeMinute = 25;
        static int breakTimeMinute = 5;
        static int breakCounter = 0;

        static int frequency = 4000;
        static int duration = 2000;
        static ConsoleColor conColor = ConsoleColor.Red;

        static string[][] signs = new string[11][];

        static void LoadSigns()
        {
            var rm = new ResourceManager("Pomodoro.Signs", Assembly.GetExecutingAssembly());
            string[] arr = rm.GetString("Signs")
                .Split(new String[] { "@\r\n" }, StringSplitOptions.None);
            for (int i = 0; i < arr.Length; i++) signs[i] = arr[i]
                .Split(new String[] { "\r\n" }, StringSplitOptions.None);
        }

        static string GetTimeAsString(int minutes, int seconds)
        {
            StringBuilder sb = new StringBuilder();
            List<int> list = new List<int>(5) { minutes / 10, minutes % 10, 10, seconds / 10, seconds % 10 };
            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < list.Count; j++)
                    sb.Append(signs[list[j]][i]);
                sb.Append("\r\n");
            }
            return sb.ToString();
        }

        //static void LoadStrings()
        //{
        //    string[] replics = File.ReadAllText("Replics.txt")
        //        .Split(new string[]{ "@\r\n" }, StringSplitOptions.None);
        //    strGreeting = replics[0];
        //    strClockingGo = replics[1];
        //    strClockingPause = replics[2];
        //    strPrinciples = replics[3];
        //    strGointToExit = replics[4];
        //}

        static void LoadStrings()
        {
            var rm = new ResourceManager("Pomodoro.Replics", Assembly.GetExecutingAssembly());
            strGreeting = rm.GetString("Greeting");
            strClockingGo = rm.GetString("ClockingGo");
            strClockingPause = rm.GetString("ClockingPause");
            strPrinciples = rm.GetString("Info");
            strGointToExit = rm.GetString("GoingToExit");
        }

        static void Breaker()
        {
            while (true)
            {
                if (isClocking)
                {
                    if (isWorkTime)
                    {
                        if (time.Minute >= workTimeMinute)
                        {
                            conColor = ConsoleColor.Green;
                            time = new DateTime();
                            isWorkTime = false;
                            breakCounter++;
                            if (breakCounter > 4)
                            {
                                breakTimeMinute = 20;
                                breakCounter = 0;
                            }
                            else breakTimeMinute = 5;
                            Console.Beep(frequency, duration);
                        }
                    }
                    else if (time.Minute >= breakTimeMinute)
                    {
                        conColor = ConsoleColor.Red;
                        time = new DateTime();
                        isWorkTime = true;
                        Console.Beep(frequency, duration);
                    }
                }
            }
        }

        static void UpdateText()
        {
            Console.Clear();
            if (isReadPrinciples) Console.WriteLine(strPrinciples);
            else if (isClocking)
            {
                if (isPause) Console.Write(strClockingPause);
                else Console.WriteLine(strClockingGo);
                Console.ForegroundColor = conColor;
                Console.Write(GetTimeAsString(time.Minute, time.Second));
                Console.ResetColor();
            }
            else if (isGoingToExit) Console.WriteLine(strGointToExit);
            else Console.WriteLine(strGreeting);
        }

        static void Listener()
        {
            while (true)
            {
                int code = (int)Console.ReadKey().KeyChar;
                Console.Clear();
                switch (code)
                {
                    //S-s
                    case 83:
                    case 115:
                        if (!isClocking)
                        {
                            isClocking = true;
                            timer.Start();
                        }
                        break;
                    //P-p
                    case 80:
                    case 112:
                        isReadPrinciples = true;
                        break;
                    //' '
                    case 32:
                        if (isClocking)
                        {
                            isPause = !isPause;
                            if (isPause) timer.Stop();
                            else timer.Start();
                        }
                        break;
                    //Y-y
                    case 89:
                    case 121:
                        if (isGoingToExit) isExit = true;
                        break;
                    //N-n
                    case 78:
                    case 110:
                        if (isGoingToExit)
                        {
                            isGoingToExit = false;
                            isExit = false;
                        }
                        break;
                    //Esc
                    case 27:
                        if (isClocking)
                        {
                            isClocking = false;
                            isPause = false;
                            time = new DateTime();
                        }
                        else if (isReadPrinciples) isReadPrinciples = false;
                        else isGoingToExit = true;
                        break;
                    default:
                        break;
                }
                UpdateText();

            }
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Loading");
            Thread readerReps = new Thread(LoadStrings);
            readerReps.Start();
            readerReps.Join();
            Thread readerSigns = new Thread(LoadSigns);
            readerSigns.Start();
            readerSigns.Join();

            Console.CursorVisible = false;
            timer = new System.Timers.Timer(1000);
            timer.Elapsed += (Object source, ElapsedEventArgs e) =>
            {
                if (!isPause)
                {
                    time = time.AddSeconds(1.0);
                    UpdateText();
                }
            };
            
            breaker.IsBackground = true;
            listener.IsBackground = true;
            breaker.Start();
            listener.Start();
            UpdateText();
            while (true) if (isExit && !isClocking) break;
        }

    }
}
