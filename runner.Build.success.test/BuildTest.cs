using gitlab_ci_runner.runner;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using gitlab_ci_runner.helper.json;

namespace runner.Build.success.test
{
    
    
    /// <summary>
    ///This is a test class for BuildTest and is intended
    ///to contain all BuildTest Unit Tests
    ///</summary>
    [TestClass()]
    public class BuildTest
    {


        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion


        /// <summary>
        ///A test for run
        ///</summary>
        [TestMethod()]
        public void runTest()
        {
            // copied from official gitlab ci runner spec
            BuildInfo buildInfo = new BuildInfo();
            buildInfo.commands = new string[] {"dir"};
            buildInfo.allow_git_fetch = false;
            buildInfo.project_id = 0;
            buildInfo.id = 9312;
            buildInfo.repo_url = "https://github.com/randx/six.git";
            buildInfo.sha = "2e008a711430a16092cd6a20c225807cb3f51db7";
            buildInfo.timeout = 1800;
            buildInfo.ref_name = "master";

            gitlab_ci_runner.runner.Build target = new gitlab_ci_runner.runner.Build(buildInfo);
            target.run();
            Console.WriteLine(target.output);
            Assert.AreEqual(target.state, State.SUCCESS);
        }
    }
}
