using NUnit.Framework;

namespace Serilog.Sinks.Raygun.Tests.Sinks.Raygun
{
    [TestFixture]
    public class OnBeforeSendActionTests
    {
        [Test]
        public void TestCreatingRaygunSinkWithOnBeforeSendAction()
        {
            var raygunSink = new RaygunSink(
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    parameters => { }
                );
            
            Assert.NotNull(raygunSink);
        }
        
    }
}