using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace Dollwright.Graybox.Editor
{
    [InitializeOnLoad]
    public static class FlowTestRunner
    {
        static readonly TestRunnerApi api;
        static readonly string Path = "Logs/DollwrightGraybox/flow-tests.json";
        static FlowTestRunner()
        {
            api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new Callbacks());
        }
        public static void Run()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before tests.");
            Directory.CreateDirectory("Logs/DollwrightGraybox");
            File.WriteAllText(Path, "{\"state\":\"running\"}");
            api.Execute(new ExecutionSettings(new Filter {
                testMode = TestMode.PlayMode, assemblyNames = new[] { "Dollwright.Graybox.Tests" }
            }));
        }
        public static void RunRoute()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before tests.");
            File.WriteAllText(Path, "{\"state\":\"running\"}");
            api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.PlayMode,
                testNames = new[] { "Dollwright.Graybox.Tests.GrayboxPlayModeTests.EntireEditedDreamIsTraversableWithThreePushSections" } }));
        }
        public static void RunUI()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before tests.");
            File.WriteAllText(Path, "{\"state\":\"running\"}");
            api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.PlayMode,
                testNames = new[] { "Dollwright.Graybox.Tests.WorkbenchFlowTests" } }));
        }
        [Serializable] sealed class Entry { public string name, result, message; public double seconds; }
        [Serializable] sealed class Report { public string state; public int passed, failed; public List<Entry> tests = new List<Entry>(); }
        sealed class Callbacks : ICallbacks
        {
            public void RunStarted(ITestAdaptor test) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }
            public void RunFinished(ITestResultAdaptor result)
            {
                var report = new Report { state = "finished", passed = result.PassCount, failed = result.FailCount };
                Collect(result, report.tests);
                File.WriteAllText(Path, JsonUtility.ToJson(report, true));
            }
            static void Collect(ITestResultAdaptor result, List<Entry> entries)
            {
                if (result.HasChildren) { foreach (var child in result.Children) Collect(child, entries); }
                else entries.Add(new Entry { name = result.Name, result = result.ResultState, message = result.Message, seconds = result.Duration });
            }
        }
    }
}
