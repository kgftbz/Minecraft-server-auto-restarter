using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.IO;

namespace ConsoleApp2
{
    class Program
    {
        public static Process ServerProcess = new Process();

        public static List<string> NewOutput = new List<string>();

        public static bool SaveOutputToFile;
        public static bool Restarting;

        public static Thread InputThread;
        public static Thread OutputThread;

        public static Thread Restart_ServerThread;

        static void Main(string[] args)
        {
            int _GCCounter = 0;

            //check for SaveOutputToFile
            SaveOutputToFile = (args.Length > 0 && args[0] == "SaveOutput");

            Log("Starting Server from Crash check program --------------");
            Log("VERSION 1.03 --------------");

            //prepare the executable
            ServerProcess.StartInfo.FileName = "bedrock_server.exe";
            ServerProcess.StartInfo.Arguments = "..";
            ServerProcess.StartInfo.RedirectStandardOutput = true;
            ServerProcess.StartInfo.RedirectStandardError = true;
            ServerProcess.StartInfo.RedirectStandardInput = true;
            ServerProcess.StartInfo.UseShellExecute = false;

            InputThread = new Thread(SendInputThread.Display);
            
            if (SaveOutputToFile)
                OutputThread = new Thread(DisplaySaveOutputThread.Display);
            else
                OutputThread = new Thread(DisplayOutputThread.Display);

            ServerProcess.Start();

            InputThread.Start();
            OutputThread.Start();

            //infinite loop
            for (;;)
            {
                //wait 60 seconds
                Thread.Sleep(60000);

                //read the new output
                for (int i = 0; i < NewOutput.Count; i++)
                {
                    //if there's any new line with a heartbeat, then it's fine
                    if (NewOutput[i].StartsWith("Server heartbeat"))
                        goto LBL_StillAlive;
                }

                Log("Main -> No heartbeat!");

                //at this point it passed 60 seconds without any heartbeat
                if (!Restarting)
                    RestartServer("Main");
                else
                    Log("Main -> already restarted");

                //wait additional time before start checking for heartbeats
                Log("Main -> Waiting extra 2 minutes");
                Thread.Sleep(120000);

            LBL_StillAlive:;
                NewOutput.Clear();

                //increase minute counter
                _GCCounter++;
                if (_GCCounter >= 10)
                {
                    //if it counted 10 minutes, reset the counter and clean some memory with the Garbage Collector
                    _GCCounter = 0;
                    GC.Collect();
                }
            }
        }

        
        public static void Log(string text)
        {
            Console.WriteLine(text);
            if(SaveOutputToFile)
                File.AppendAllText("ServerOutput.log", text + "\r\n");
        }

        public static void RestartServer(string callingThread)
        {
            Log(callingThread + " -> called RestartServer()");

            //the new output didn't had any heartbeat!
            Log("Server Crash Detected! Restarting Server --------------");

            Restarting = true;
           // InputThread.Interrupt();
           // OutputThread.Interrupt();

            //end current process and restart
            try
            {
                ServerProcess.Kill();
                ServerProcess.Close();
            }
            catch (Exception e)
            {
                Log("Following error is ignorable ---------------");
                Log(e.Message);
                Log(e.StackTrace);

                Log("Ignorable error, Restarting Server Normally ---");
            }

            ServerProcess.Start();

            //  InputThread.Start();
            // OutputThread.Start();

            Restart_ServerThread = new Thread(End_Restart.EndRestart);
            Restart_ServerThread.Start();
        }



        //Thread to redirect input
        public class SendInputThread
        {
            public static void Display()
            {
                string str;

                for (; ; )
                {
                    try
                    {
                        str = Console.ReadLine();
                        ServerProcess.StandardInput.WriteLine(str);
                    }
                    catch (Exception e)
                    {
                        if (!Restarting)
                        {
                            Log("SendInputThread -> --- An Error Occured! ---");
                            Log(e.Message);
                            Log(e.StackTrace);

                            RestartServer("SendInputThread");
                        }
                    }
                }
            }
        }

        //Thread to redirect output
        public class DisplayOutputThread
        {
            public static void Display()
            {
                string str;

                for (; ; )
                {
                    try
                    {
                        if (Restarting)
                            Thread.Sleep(1000);
                        else
                            Thread.Sleep(100);

                        while (!ServerProcess.StandardOutput.EndOfStream)
                        {
                            str = ServerProcess.StandardOutput.ReadLine();
                            Console.WriteLine(str);
                            NewOutput.Add(str);
                        }
                    }
                    catch (Exception e)
                    {
                        if (!Restarting)
                        {
                            Log("DisplayOutputThread -> --- An Error Occured! ---");
                            Log(e.Message);
                            Log(e.StackTrace);

                            RestartServer("DisplayOutputThread");
                        }

                        /*if (!Restarting)
                            RestartServer("DisplayOutputThread");
                        else
                            Log("DisplayOutputThread -> Ignorable error");*/
                    }
                }

            }
        }
        //Thread to redirect output and save it
        public class DisplaySaveOutputThread
        {
            public static void Display()
            {
                string str;

                for (; ; )
                {
                    try
                    {
                        if (Restarting)
                            Thread.Sleep(1000);
                        else
                            Thread.Sleep(100);

                        while (!ServerProcess.StandardOutput.EndOfStream)
                        {
                            str = ServerProcess.StandardOutput.ReadLine();
                            Console.WriteLine(str);
                            NewOutput.Add(str);
                            File.AppendAllText("ServerOutput.log", str + "\r\n");
                        }
                    }
                    catch (Exception e)
                    {
                        if (!Restarting)
                        {
                            Log("DisplaySaveOutputThread -> --- An Error Occured! ---");
                            Log(e.Message);
                            Log(e.StackTrace);

                            RestartServer("DisplaySaveOutputThread");
                        }
                    }
                }
            }
        }

        public class End_Restart
        {
            public static void EndRestart()
            {
                //wait additional time before start checking for heartbeats
                Console.WriteLine("End_Restart thread -> waiting 3 extra minutes while restarting");
                Thread.Sleep(180000);
                Console.WriteLine("End_Restart thread -> finished waiting 3 extra minutes / Restarting state set to false");
                Restarting = false;
            }
        }

    }
}
