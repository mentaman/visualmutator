﻿namespace VisualMutator.Tests.Operators
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Security.Policy;
    using CommonUtilityInfrastructure;
    using Extensibility;
    using Microsoft.Cci;
    using Model;
    using Model.Decompilation;
    using Model.Decompilation.CodeDifference;
    using Model.Mutations;
    using Model.Mutations.MutantsTree;
    using Model.Mutations.Operators;
    using Roslyn.Compilers;
    using Roslyn.Compilers.CSharp;
    using log4net;

    public class Common
    {
        protected static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static void RunMutations(string code, IMutationOperator oper, out List<Mutant> mutants,
                                        out AssembliesProvider original, out CodeDifferenceCreator diff)
        {
            _log.Info("Common.RunMutations configuring for " + oper + "...");

            var cci = new CommonCompilerAssemblies();
            var utils = new OperatorUtils(cci);

            var container = new MutantsContainer(cci, utils);
            var visualizer = new CodeVisualizer(cci);
            var cache = new MutantsCache(container);


            container.DebugConfig = true;
            var mutmods = CreateMutants(CreateModule(code), oper, container, cci);
            mutants = mutmods.Select(m => m.Mutant).ToList();

            original = new AssembliesProvider(cci.Modules);

            cache.Initialize(original, new List<TypeIdentifier>());
            diff = new CodeDifferenceCreator(cache, visualizer);

            Console.WriteLine("ORIGINAL:");
            string listing = diff.GetListing(CodeLanguage.CSharp, original);
            Console.WriteLine(listing);
        }
        public static void RunMutationsReal(string assemblyPath, IMutationOperator oper, out List<MutMod> mutants,
                                        out AssembliesProvider original, out CodeVisualizer visualizer, 
                                        out CommonCompilerAssemblies cci)
        {
            _log.Info("Common.RunMutations configuring for " + oper + "...");

            cci = new CommonCompilerAssemblies();
            var utils = new OperatorUtils(cci);

            var container = new MutantsContainer(cci, utils);
            visualizer = new CodeVisualizer(cci);
            var cache = new MutantsCache(container);


            container.DebugConfig = true;
            original = new AssembliesProvider(cci.Modules);

            var diff = new CodeDifferenceCreator(cache, visualizer);
            cache.Initialize(original, new List<TypeIdentifier>());

            Console.WriteLine("ORIGINAL:");
            string listing = diff.GetListing(CodeLanguage.CSharp, original);
            //  string listing = visualizer.Visualize(CodeLanguage.CSharp,)
            Console.WriteLine(listing);


            mutants = CreateMutantsExt(assemblyPath, oper, container, cci, visualizer, original);
       


        }
        public static string CreateModule(string code)
        {
            _log.Info("Parsing test code...");
            SyntaxTree tree = SyntaxTree.ParseText(code);
            _log.Info("Creating compilation...");
            Compilation comp = Compilation.Create("MyCompilation",
                                                  new CompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .AddSyntaxTrees(tree)
                .AddReferences(new MetadataFileReference(typeof (object).Assembly.Location));

            string outputFileName = Path.Combine(Path.GetTempPath(), "MyCompilation.lib");
            var ilStream = new FileStream(outputFileName, FileMode.OpenOrCreate);
            _log.Info("Emiting file...");
            // var pdbStream = new FileStream(Path.ChangeExtension(outputFileName, "pdb"), FileMode.OpenOrCreate);
            //  _log.Info("Emiting pdb file...");

            EmitResult result = comp.Emit(ilStream);
            ilStream.Close();
            if (!result.Success)
            {
                string aggregate = result.Diagnostics.Select(a => a.Info.GetMessage()).Aggregate((a, b) => a + "\n" + b);
                throw new InvalidProgramException(aggregate);
            }
            return outputFileName;
        }

        public static string CreateModule2(string code)
        {
            var e = new Evidence();
            return null;
        }

        public static List<MutMod> CreateMutants(string filePath, IMutationOperator operatorr,
                                                 MutantsContainer container, CommonCompilerAssemblies cci)
        {
            cci.AppendFromFile(filePath);
            _log.Info("Copying assemblies...");
            List<IModule> copiedModules = cci.Modules.Select(cci.Copy).Cast<IModule>().ToList();


            MutantsContainer.OperatorWithTargets operatorWithTargets = container.FindTargets(operatorr,
                                                                                             copiedModules,
                                                                                             new List<TypeIdentifier>());

            var mutants = new List<MutMod>();
            foreach (MutationTarget mutationTarget in operatorWithTargets.MutationTargets.Values.SelectMany(v => v))
            {
                var exec = new ExecutedOperator("", "", operatorWithTargets.Operator);
                var mutant = new Mutant("0", exec, mutationTarget, operatorWithTargets.CommonTargets);

                var assembliesProvider = container.ExecuteMutation(mutant, cci.Modules, new List<TypeIdentifier>(), ProgressCounter.Inactive());
                mutants.Add(new MutMod ( mutant, assembliesProvider ));
            
            

            
            
            }
            return mutants;
        }
        public static List<MutMod> CreateMutantsExt(string filePath, IMutationOperator operatorr, MutantsContainer container, CommonCompilerAssemblies cci, CodeVisualizer visualizer, AssembliesProvider original)
        {
            cci.AppendFromFile(filePath);
            _log.Info("Copying assemblies...");
            List<IModule> copiedModules = cci.Modules.Select(cci.Copy).Cast<IModule>().ToList();


            MutantsContainer.OperatorWithTargets operatorWithTargets = container.FindTargets(operatorr,
                                                                                             copiedModules,
                                                                                             new List<TypeIdentifier>());

            var mutants = new List<MutMod>();
            foreach (MutationTarget mutationTarget in operatorWithTargets.MutationTargets.Values.SelectMany(v => v))
            {
                var exec = new ExecutedOperator("", "", operatorWithTargets.Operator);
                var mutant = new Mutant("0", exec, mutationTarget, operatorWithTargets.CommonTargets);

                var assembliesProvider = container.ExecuteMutation(mutant, cci.Modules, new List<TypeIdentifier>(), ProgressCounter.Inactive());
                mutants.Add(new MutMod ( mutant, assembliesProvider ));


                string code = visualizer.Visualize(CodeLanguage.CSharp, mutant.MutationTarget,
                                                                                     assembliesProvider);
                Console.WriteLine(code);


                cci.WriteToFile(assembliesProvider.Assemblies.First(), @"D:\PLIKI\mutest.dll");
            
            
            }
            return mutants;
        }


       
    }
     public class MutMod
     {
         public Mutant Mutant { get; set; }
         public AssembliesProvider AssembliesProvider { get; set; }

         public MutMod(Mutant mutant, AssembliesProvider assembliesProvider)
         {
             Mutant = mutant;
             AssembliesProvider = assembliesProvider;
         }
     }

}