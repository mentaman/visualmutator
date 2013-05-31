﻿namespace VisualMutator.Tests.Operators
{
    using System;
    using System.Collections.Generic;
    using Model;
    using Model.Decompilation;
    using Model.Decompilation.CodeDifference;
    using Model.Mutations.MutantsTree;
    using NUnit.Framework;
    using OperatorsObject.Operators;
    using OperatorsStandard;
    using Util;
    using log4net.Appender;
    using log4net.Config;
    using log4net.Layout;

    [TestFixture]
    public class EHC_Test
    {
        #region Setup/Teardown

        [SetUp]
        public void Setup()
        {
            BasicConfigurator.Configure(
                new ConsoleAppender
                    {
                        Layout = new SimpleLayout()
                    });
        }

        #endregion

  
        [Test]
        public void MutationSuccess()
        {
            const string code =
                @"using System;
namespace Ns
{
    public class Test
    {
        public int Method1(int a, int b)
        {
            int x = a;
            try
            {
                x = a - b;
            }
            catch(InvalidOperationException e1)
            {
                x = a + b*2;
            }
            catch(InvalidCastException)
            {
                x = a + b*3;
            }
            catch(NullReferenceException e2)
            {
                throw new NullReferenceException();
            }
            catch(Exception e3)
            {
                throw e3;
            }
            finally
            {
                x = a+b;
            }
            return x;
        }
    }
}";
            Common.DebugTraverse(code);
            List<Mutant> mutants;
            AssembliesProvider original;
            CodeDifferenceCreator diff;
            Common.RunMutations(code, new EHC_ExceptionHandlingChange(), out mutants, out original, out diff);

            foreach (Mutant mutant in mutants)
            {
                CodeWithDifference codeWithDifference = diff.CreateDifferenceListing(CodeLanguage.CSharp, mutant,
                                                                                     original);
                Console.WriteLine(codeWithDifference.Code);

                //   codeWithDifference.LineChanges.Count.ShouldEqual(2);
            }

            mutants.Count.ShouldEqual(2);
        }
    }
}