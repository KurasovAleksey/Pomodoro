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

using System.Windows.Forms;

namespace Pomodoro
{
    class Program
    {
        #region Variables

        static TimeSpan time = new TimeSpan(0, 0, 0);

        static StringBuilder sb = new StringBuilder();

        static volatile bool isClocking = false;
        static volatile bool isPause = false;
        static volatile bool isExit = false;
        static volatile bool isGoingToExit = false;
        static volatile bool isReadPrinciples = false;
        static volatile bool isWorkTime = true;
        static volatile bool isBigBreak = false;

        static Thread listener = new Thread(Listener);
        static Thread breaker = new Thread(Breaker);
        static Thread clocker = new Thread(Clock);
        static DateTime lastTime;

        static string strGreeting;
        static string strClockingGo;
        static string strClockingPause;
        static string strPrinciples;
        static string strGoingToExit;

        static int breakTimeMinute;
        static int workTimeMinute = 25;
        static int shortBreakTimeMinute = 5;
        static int bigBreakTimeMinute = 30;
        static int breakCounter = 0;

        static int frequency = 4000;
        static int duration = 2000;
        static ConsoleColor conColor = ConsoleColor.Red;

        static string[][] signs = new string[11][];
        static List<int> list;

        #endregion

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
            sb.Clear();
            list = new List<int>(5) { minutes / 10, minutes % 10, 10, seconds / 10, seconds % 10 };
            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < list.Count; j++)
                    sb.Append(signs[list[j]][i]);
                sb.Append("\r\n");
            }
            return sb.ToString();
        }
       
        static void LoadStrings()
        {
            var rm = new ResourceManager("Pomodoro.Replics", Assembly.GetExecutingAssembly());
            strGreeting = rm.GetString("Greeting");
            strClockingGo = rm.GetString("ClockingGo");
            strClockingPause = rm.GetString("ClockingPause");
            strPrinciples = rm.GetString("Info");
            strGoingToExit = rm.GetString("GoingToExit");
        }

        static void Clock()
        {
            while (true)
            {
                if (isClocking && !isPause)
                {
                    DateTime dt = DateTime.Now;
                    time += (dt - lastTime);
                    lastTime = dt;
                    Thread.Sleep(500);
                    UpdateText();
                }
            }
        }

        static void Breaker()
        {
            while (true)
            {
                if (isClocking)
                {
                    if (isWorkTime)
                    {
                        if (time.Minutes >= workTimeMinute)
                        {
                            conColor = ConsoleColor.Green;
                            time = new TimeSpan(0, 0, 0);
                            isWorkTime = false;
                            breakCounter++;
                            if (breakCounter > 4)
                            {
                                isBigBreak = true;
                                breakTimeMinute = bigBreakTimeMinute;
                                breakCounter = 0;
                            }
                            else
                            {
                                isBigBreak = false;
                                breakTimeMinute = shortBreakTimeMinute;
                            }
                            Console.Beep(frequency, duration);
                        }
                    }
                    else if (time.Minutes >= breakTimeMinute)
                    {
                        conColor = ConsoleColor.Red;
                        time = new TimeSpan(0, 0, 0);
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
                if (isPause) Console.WriteLine(strClockingPause);
                else if(isBigBreak) Console.WriteLine(strClockingGo + '\n' + "Big break!");
                else Console.WriteLine(strClockingGo + '\n');
                Console.ForegroundColor = conColor;
                Console.Write(GetTimeAsString(time.Minutes, time.Seconds));
                Console.ResetColor();
            }
            else if (isGoingToExit) Console.WriteLine(strGoingToExit);
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
                            lastTime = DateTime.Now;
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
                            lastTime = DateTime.Now;
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
                            time = new TimeSpan(0, 0, 0);
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
            Thread readerSigns = new Thread(LoadSigns);
            readerReps.Start();
            readerSigns.Start();
            readerReps.Join();
            readerSigns.Join();

            Console.CursorVisible = false;

            breaker.IsBackground = true;
            listener.IsBackground = true;
            clocker.IsBackground = true;
            breaker.Start();
            listener.Start();
            clocker.Start();
            UpdateText();
          
            while (true) if (isExit) break;
        }

    }
}
