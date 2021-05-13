using Microsoft.VisualStudio.TestTools.UnitTesting;
using Synchronizer;
using System;

namespace UnitTest
{
    [TestClass]
    public class UnitTestSynchronizer
    {
        [TestMethod]
        public void TestMethodSyncRequest()
        {
            Synchronizer<RequestInput, RequestOutput, Response, string> synchronizer = new Synchronizer<RequestInput, RequestOutput, Response, string>(
                input => new RequestOutput() { Result = input.Arg % 2 == 0 },
                (input, output) => output.Result,
                (input, output) => input.SN,
                response => response.NS);

            RequestInput requestInput = new RequestInput();
            RequestOutput requestOutput = null;
            Response response = null;
            SyncStatus syncStatus;

            string guid = Guid.NewGuid().ToString("N");
            syncStatus = synchronizer.SyncRequest(new RequestInput() { Arg = 1, SN = guid }, out requestOutput, out response, TimeSpan.FromSeconds(5));
            Assert.IsTrue(syncStatus == SyncStatus.Failed);
            Assert.IsTrue(requestOutput.Result == false);
            Assert.IsNull(response);
        }
    }
}
