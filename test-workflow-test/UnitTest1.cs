using test_workflow;

namespace test_workflow_test
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            ChildModel child = new ChildModel(10)
            {
                Growable = false,
                Gender = 1
            };
            Assert.AreEqual(10, child.Age);
        }
        
        [TestMethod]
        public void TestMethod2()
        {
            ChildModel child = new ChildModel(10)
            {
                Growable = false,
                Gender = 1
            };
            Assert.AreEqual(1, child.Gender);
        }
        
        [TestMethod]
        public void TestMethod3()
        {
            ChildModel child = new ChildModel(22)
            {
                Growable = false,
                Gender = 1
            };
            Assert.AreEqual(22, child.Age);
        }
    }
}