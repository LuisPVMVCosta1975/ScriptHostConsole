using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using LCSoft.Framework.Scripting;
using LCSoft.Framework.Scripting.ValueContainer;
using LCSoft.Framework.Scripting.Debugger.WPF;

namespace ScriptHostConsole;

class Program
{
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint GetConsoleProcessList(uint[] processList, uint processCount);

    [DllImport("kernel32.dll")]
    private static extern int QueryPerformanceCounter(ref Int64 lpPerformanceCount);

    public static Int64 ProgressBar(String Header, Int64 Count, Int64 Total, Int64 Anterior)
    {
        Int64 Percentagem = (Int64)((Double)Count / (Double)Total * (Double)100);
        if (Anterior != Percentagem)
        {
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(Header);
            Console.Write(Percentagem);
            Console.Write("% ");
        }
        return Percentagem;
    }

    public static void Beep()
    {
        Console.Beep();
    }
    public static void Beep(Int64 Frequency, Int64 Duration)
    {
        Console.Beep((Int32)Frequency, (Int32)Duration);
    }

    public static void Pause()
    {
        Console.WriteLine("Carregue em qualquer tecla para continuar.");
        Console.ReadKey(true);
    }
    public static void Pause(String Echo)
    {
        Console.WriteLine(Echo);
        Console.ReadKey(true);
    }

    public static void Write(String Echo)
    {
        Console.Write(Echo);
    }
    public static void WriteLine()
    {
        Console.WriteLine();
    }
    public static void WriteLine(String Echo)
    {
        Console.WriteLine(Echo);
    }
    public static void WriteLine(Object Obj)
    {
        //if (Obj is String)
        //{
        //    WriteLine((String)Obj);
        //    return;
        //}

        Console.WriteLine(Obj);
    }

    public static Boolean IsStandAlone()
    {
        return (GetConsoleProcessList(new uint[1], 1) == 1);
    }

    public static Int32 ReadAsInt32()
    {
        return Console.Read();
    }
    public static Char ReadAsChar()
    {
        return Console.ReadKey().KeyChar;
    }
    public static Char ReadYesNo()
    {
        Char Char;
        do Char = Console.ReadKey(true).KeyChar;
        while (!"SsNn".Contains(Char.ToString()));
        Console.Write(Char);
        return Char;
    }
    public static Char ReadOneKey(String Keys)
    {
        Char Char;
        do Char = Console.ReadKey(true).KeyChar;
        while (!Keys.Contains(Char.ToString()));
        Console.Write(Char);
        return Char;
    }
    public static String ReadLine()
    {
        return Console.ReadLine();
    }

    public static void Clear()
    {
        Console.Clear();
    }
    public static void ClearLine()
    {
        Console.SetCursorPosition(0, Console.CursorTop);
        Console.Write(new String(' ', Console.WindowWidth - 1));
        Console.SetCursorPosition(0, Console.CursorTop);
    }

    public static void Title(String Title, Boolean Echo)
    {
        if (Echo) Console.WriteLine(Title);
        Console.Title = Title;
    }

    public static void Main(String[] args)
    {
        Console.OutputEncoding = UTF8Encoding.Unicode;

        Console.WriteLine("Script Host Console, R16, ©LCSoft 2010-2025");
        Console.WriteLine(LCSoft.Framework.Scripting.Script.Sign());

        if (args.Length == 0)
        {
            Console.WriteLine("Uso: ScriptHostConsole <ScriptFullFileName> [<Argument>](0+)\r\n");
            return;
        }

        Console.Write("Loading script...");

        SetCurrentDirectory(args[0]);
        Script Script = Script.FromFile(args[0]);

        ClearLine();

        Context Context = new();
        Context.SetVariable("Host", typeof(Program));
#if (DEBUG)
        Context.SetVariable("IsDebug", true, null);
#else
        Context.SetVariable("IsDebug", false, null);
#endif
#if (RELEASE)
        Context.SetVariable("IsRealease", true, null);
#else
        Context.SetVariable("IsRealease", false, null);
#endif
        Context.SetVariable("ParameterCount", (Int64)(args.Length - 1), null);
        for (Int32 i = 1; i < args.Length; i++)
        {
            Context.SetVariable("Parameter" + i.ToString(), args[i], null);
        }

        ScriptingConfiguration.SetBreakpointHandler(BreakpointHandler);

        Script.Run(Context);
    }

    private static void BreakpointHandler(Context Context, String ID, List<ValueContainerBase> Values)
    {
        ManualResetEvent MRE = new(false);
        StartSingleThreadedApartmentThread(() =>
        {
            OpenDebugger(MRE, ID, Context, Values);
        });
        MRE.WaitOne();
    }

    private static void StartSingleThreadedApartmentThread(ThreadStart Action)
    {
        Thread Thread = new(Action);
        Thread.SetApartmentState(ApartmentState.STA);
        Thread.Start();
    }

    private static void OpenDebugger(ManualResetEvent MRE, String ID, Context Context, List<ValueContainerBase> Values)
    {
        Debugger Debugger = new();
        Debugger.SetUp(ID, Values, Context);
        Debugger.ShowDialog();
        MRE.Set();
    }

    private static void SetCurrentDirectory(String ScriptFullFileName)
    {
        String? Directory = Path.GetDirectoryName(ScriptFullFileName);
        if (Directory != null && Directory != "")
        {
            Environment.CurrentDirectory = Directory;
        }
    }
}