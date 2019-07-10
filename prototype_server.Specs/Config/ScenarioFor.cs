//using NUnit.Framework;
//using Story = Specify.Stories.Story;
//
//namespace prototype_server.Specs.Config
//{
//    /// <summary>
//    /// The base class for scenarios without a story (normally unit test scenarios).
//    /// </summary>
//    /// <typeparam name="TSut">The type of the t sut.</typeparam>
//    [TestFixture] 
//    public abstract class ScenarioFor<TSut> : Specify.ScenarioFor<TSut>
//        where TSut : class
//    {
//        [Test]
//        public override void Specify()
//        {
//            base.Specify();
//        }
//    }
//
//    /// <summary>
//    /// The base class for scenarios with a story (BDD-style acceptance tests).
//    /// </summary>
//    /// <typeparam name="TSut">The type of the SUT.</typeparam>
//    /// <typeparam name="TStory">The type of the t story.</typeparam>
//    [TestFixture]
//    public abstract class ScenarioFor<TSut, TStory> : Specify.ScenarioFor<TSut, TStory>
//        where TSut : class
//        where TStory : Story, new()
//    {
//        [Test]
//        public override void Specify()
//        {
//            base.Specify();
//        }
//    }
//}