﻿namespace VisualMutator.Model.Tests.Services
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using System.Xml;
    using System.Xml.Linq;
    using Infrastructure;
    using log4net;
    using NUnit.Core;
    using RunProcessAsTask;
    using TestsTree;
    using UsefulTools.Core;
    using UsefulTools.ExtensionMethods;
    using UsefulTools.Paths;

    public interface INUnitExternal
    {
        Task<List<MyTestResult>> RunTests(string nunitConsolePath, 
            string inputFile, string outputFile,
            SelectedTests selectedTests);
    }

    public class NUnitExternal : INUnitExternal
    {
        private readonly CommonServices _svc;
        private readonly IProcesses _processes;

        public NUnitExternal(CommonServices svc, IProcesses processes)
            : base()
        {
            _svc = svc;
            _processes = processes;
            
        }

        private ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public Task<List<MyTestResult>> RunTests(string nunitConsolePath, 
            string inputFile, string outputFile,
            SelectedTests selectedTests)
        {
            _log.Debug("Running tests on: " + inputFile);
            Task<ProcessResults> results = RunNUnitConsole(nunitConsolePath, inputFile, outputFile, selectedTests);

            return results.ContinueWith(testResult =>
            {

                if (testResult.Exception != null)
                {
                    _log.Error(testResult.Exception);
                    return new List<MyTestResult>();
                }
                else if (!_svc.FileSystem.File.Exists(outputFile))
                {
                    _log.Error("Test results in file: " + outputFile+" not found.");
                    return new List<MyTestResult>();
                }
                else
                {
                    Dictionary<string, MyTestResult> tresults = ProcessResultFile(outputFile);
                    return tresults.Values.ToList();
                }
            }); 
        }

        public Task<ProcessResults> RunNUnitConsole(string nunitConsolePath, 
            string inputFile, string outputFile,
            SelectedTests selectedTests)
        {
            var listpath = new FilePathAbsolute(inputFile)
                .GetBrotherFileWithName(
                Path.GetFileNameWithoutExtension(inputFile) + "-Runlist.txt").Path;

         //   var listpath = Path.Combine(Path.GetTempPath(), "visualMutator-Runlist.txt");
            using(var file = File.CreateText(listpath))
            {
                foreach (var str in selectedTests.TestsDescription.Split(' '))
                {
                    file.WriteLine(str.Trim());
                }
            }
           // string testToRun = (selectedTests.Count == 0 ? "": " -run " + 
             //  string.Join(",",selectedTests.Cast<NUnitTestId>().Select(id => id.TestName.FullName)));
            string testToRun = " /runlist:" + listpath.InQuotes() + " ";
            string arg =  inputFile.InQuotes()
                         + testToRun
                         + " /xml \"" + outputFile + "\" /nologo -trace=Verbose";
              //  + " -framework net-4.0";

            _log.Info("Running " + nunitConsolePath + " with args: " + arg);
            var startInfo = new ProcessStartInfo
            {
                Arguments = arg,
                CreateNoWindow = true,
                ErrorDialog = true,
                RedirectStandardOutput = false,
                FileName = nunitConsolePath,
                UseShellExecute = false,
            };

            return _processes.RunAsync(startInfo);
        }
        
        public Dictionary<string, MyTestResult> ProcessResultFile( string fileName)
        {
            _log.Debug("Processing result file: " + fileName);
            XmlReaderSettings s = new XmlReaderSettings();
            s.CheckCharacters = false;
            Dictionary<string, MyTestResult> resultDictionary = new Dictionary<string, MyTestResult>();
            XmlReader reader = XmlReader.Create(fileName, s);

            var nsStack = new Stack<String>();
            bool isParametrizedTest = false;
            TestNodeClass currentClass = null;
            try
            {
                while (reader.Read())
                {
                    if (reader.Name == "test-suite")
                    {
                        if (reader.GetAttribute("type") == "Namespace" && reader.NodeType == XmlNodeType.Element)
                        {
                            nsStack.Push(reader.GetAttribute("name"));
                        }
                        if (reader.GetAttribute("type") == "Namespace" && reader.NodeType == XmlNodeType.EndElement)
                        {
                            nsStack.Pop();
                        }

                        if (reader.NodeType == XmlNodeType.EndElement)
                        {
                            isParametrizedTest = false;
                            currentClass = null;
                        }
                        else if (reader.NodeType == XmlNodeType.Element)
                        {
                            if (reader.GetAttribute("type") == "ParameterizedTest")
                            {
                                isParametrizedTest = true;
                            }
                            if (reader.GetAttribute("type") == "TestFixture")
                            {
                                isParametrizedTest = false;
                                currentClass = new TestNodeClass(reader.GetAttribute("name"));
                                currentClass.Namespace = nsStack.Aggregate((a, b) => a + "." + b);
                            }
                        }
                    }
                    else if (reader.Name == "test-case" && reader.NodeType == XmlNodeType.Element)
                    {
                        var node = (XElement)XNode.ReadFrom(reader);

                        GetValue(resultDictionary, node, isParametrizedTest);
                    }
                
                }
            }
            catch (Exception e)
            {
                _log.Error("Log file parsing error", e);
                throw;
            }
            
            reader.Close();
            return resultDictionary;
        }
        
        private void GetValue(Dictionary<string, MyTestResult> resultDictionary, XElement test, bool isParametrizedTest)
        {
            
            try
            {
                String testName = test.Attribute("name").Value;
                if (isParametrizedTest)
                {
                    int paranIdx = testName.IndexOf('(');
                    if (paranIdx > 0)
                        testName = testName.Substring(0, paranIdx);
                }
                String testRes = "False";
                

                var result = new MyTestResult(test.Attribute("name").Value);
                if (test.Attribute("success") != null)
                {
                    result.Success = test.Attribute("success").Value == "True";
                }
                _log.Debug("Found test case: " + testName + " with success: " + result.Success);
                if (!result.Success)
                {
                    result.Message = test.Descendants(XName.Get("message", "")).Single().Value;
                    result.StackTrace = test.Descendants(XName.Get("stack-trace", "")).Single().Value;
                }

                resultDictionary.Add(result.Name, result);
              
            }
            catch (Exception e)
            {
                _log.Error("Log file parsing error", e);
            }
        }
     
    }
}