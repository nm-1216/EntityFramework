// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.EntityFrameworkCore.Benchmarks.EF6.Models.AdventureWorks;
using Microsoft.EntityFrameworkCore.Benchmarks.Models.AdventureWorks;
using Microsoft.EntityFrameworkCore.Benchmarks.Models.AdventureWorks.TestHelpers;
using System;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using BenchmarkDotNet.Attributes;
// ReSharper disable InconsistentNaming
// ReSharper disable ReturnValueOfPureMethodIsNotUsed

namespace Microsoft.EntityFrameworkCore.Benchmarks.EF6
{
    public class InitializationTests
    {
        private ColdStartSandbox _sandbox;
        private ColdStartEnabledTests _testClass;

        [Params(true, false)]
        public bool Cold { get; set; }

        [GlobalSetup]
        public void Initialize()
        {
            if (Cold)
            {
                _sandbox = new ColdStartSandbox();
                _testClass = _sandbox.CreateInstance<ColdStartEnabledTests>();
            }
            else
            {
                _testClass = new ColdStartEnabledTests();
            }
        }

        [GlobalCleanup]
        public void CleanupContext()
        {
            _sandbox?.Dispose();
        }

        [Benchmark]
        public void CreateAndDisposeUnusedContext()
        {
            _testClass.CreateAndDisposeUnusedContext(Cold ? 1 : 10000);
        }

        [Benchmark]
        public void InitializeAndQuery_AdventureWorks()
        {
            _testClass.InitializeAndQuery_AdventureWorks(Cold ? 1 : 1000);
        }

        [Benchmark]
        public void InitializeAndSaveChanges_AdventureWorks()
        {
            _testClass.InitializeAndSaveChanges_AdventureWorks(Cold ? 1 : 100);
        }

        [Benchmark]
        public void BuildModel_AdventureWorks()
        {
            var builder = new DbModelBuilder();
            AdventureWorksContext.ConfigureModel(builder);
            builder.Build(new SqlConnection(AdventureWorksFixtureBase.ConnectionString));
        }

        private class ColdStartEnabledTests : MarshalByRefObject
        {
            public void CreateAndDisposeUnusedContext(int count)
            {
                for (var i = 0; i < count; i++)
                {
                    // ReSharper disable once UnusedVariable
                    using (var context = AdventureWorksFixture.CreateContext())
                    {
                    }
                }
            }

            public void InitializeAndQuery_AdventureWorks(int count)
            {
                for (var i = 0; i < count; i++)
                {
                    using (var context = AdventureWorksFixture.CreateContext())
                    {
                        context.Department.First();
                    }
                }
            }

            public void InitializeAndSaveChanges_AdventureWorks(int count)
            {
                for (var i = 0; i < count; i++)
                {
                    using (var context = AdventureWorksFixture.CreateContext())
                    {
                        context.Currency.Add(new Currency
                        {
                            CurrencyCode = "TMP",
                            Name = "Temporary"
                        });

                        using (context.Database.BeginTransaction())
                        {
                            context.SaveChanges();

                            // TODO: Don't mesure transaction rollback
                        }
                    }
                }
            }
        }
    }
}