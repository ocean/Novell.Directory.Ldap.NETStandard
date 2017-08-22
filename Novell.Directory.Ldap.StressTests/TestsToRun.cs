using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Novell.Directory.Ldap.NETStandard.FunctionalTests;
using Xunit;

namespace Novell.Directory.Ldap.StressTests
{
    public static class TestsToRun
    {
        static TestsToRun()
        {
            Tests.AddRange(LoadTests());
        }

        public static List<Action> LoadTests()
        {
            return GetMethods()
                .Select(m =>
                {
                    var testType = m.DeclaringType;
                    var testInstance = Activator.CreateInstance(testType);
                    return new Action(() => { m.Invoke(testInstance, null); });
                }).ToList();
        }

        public static List<MethodInfo> GetMethods()
        {
            var testsAssembly = typeof(AddEntryTests).GetTypeInfo().Assembly;
            return
                testsAssembly.DefinedTypes.Where(x => x.Name.EndsWith("Tests"))
                    .SelectMany(t => t.GetMethods())
                    .Where(m => m.IsPublic)
                    .Where(m => m.CustomAttributes.Any(attr => attr.AttributeType.Name == typeof(FactAttribute).Name 
                        && attr.NamedArguments.Count(na => na.MemberName == "Skip") == 0))
                    .ToList();
        }

        public static List<Action> Tests = new List<Action>();
    }
}