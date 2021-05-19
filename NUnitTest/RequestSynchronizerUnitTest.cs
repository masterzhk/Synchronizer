using NUnit.Framework;
using Synchronizer;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NUnitTest
{
    public class RequestSynchronizerUnitTest
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        [TestCase("guid001", -1, 3, false, "guid001", "-1", 1, SyncStatus.Failed, null)]
        [TestCase("guid002", -4, 3, true, "guid002", "-4", 1, SyncStatus.Successful, "-4")]
        [TestCase("guid003", 0, 3, true, "guid003", "0", 1, SyncStatus.Successful, "0")]
        [TestCase("guid004", 1, 3, false, "guid004", "1", 1, SyncStatus.Failed, null)]
        [TestCase("guid005", 4, 3, true, "guid005", "4", 1, SyncStatus.Successful, "4")]
        [TestCase("guid006", 4, 3, true, "guid006", "4", 0, SyncStatus.Successful, "4")]
        [TestCase("guid007", 4, 3, true, "guid007", "4", 2, SyncStatus.Successful, "4")]
        [TestCase("guid008", 4, 3, true, "guid008", "4", 4, SyncStatus.Timeout, null)]
        [TestCase("guid009", 4, 3, true, "guid009", "4", 5, SyncStatus.Timeout, null)]
        [TestCase("guid010", 4, 0, true, "guid010", "4", 1, SyncStatus.Timeout, null)]
        public void SyncRequestWithRequestOutputTestMethod(
            string requestInputSn,
            int requestInputArg,
            int syncTimeout,
            bool requestOutputValue,
            string responseSn,
            string responseValue,
            int responseDelay,
            SyncStatus syncStatus,
            string responseValueSynced
            )
        {
            RequestSynchronizer<RequestInput, RequestOutput, Response, string> synchronizer = new RequestSynchronizer<RequestInput, RequestOutput, Response, string>(
                input => new RequestOutput() { Result = input.Arg % 2 == 0 },
                (input, output) => output.Result,
                (input, output) => input.SN,
                response => response.NS);

            RequestInput requestInput = new RequestInput() { SN = requestInputSn, Arg = requestInputArg };

            Task.Run(() =>
            {
                Thread.Sleep(TimeSpan.FromSeconds(responseDelay));
                synchronizer.FeedResponse(new Response() { NS = responseSn, Value = responseValue });
            });

            var actSyncStatus = synchronizer.SyncRequest(requestInput, out RequestOutput requestOutput, out Response response, TimeSpan.FromSeconds(syncTimeout));

            Assert.IsNotNull(requestOutput);
            Assert.IsTrue(requestOutputValue == requestOutput.Result);
            Assert.IsTrue(actSyncStatus == syncStatus);
            Assert.IsTrue(response?.Value == responseValueSynced);
        }

        [Test]
        [TestCase("guid001", -1, 3, false, "guid001", "-1", 1, SyncStatus.Failed, null)]
        [TestCase("guid002", -4, 3, true, "guid002", "-4", 1, SyncStatus.Successful, "-4")]
        [TestCase("guid003", 0, 3, true, "guid003", "0", 1, SyncStatus.Successful, "0")]
        [TestCase("guid004", 1, 3, false, "guid004", "1", 1, SyncStatus.Failed, null)]
        [TestCase("guid005", 4, 3, true, "guid005", "4", 1, SyncStatus.Successful, "4")]
        [TestCase("guid006", 4, 3, true, "guid006", "4", 0, SyncStatus.Timeout, null)]
        [TestCase("guid007", 4, 3, true, "guid007", "4", 2, SyncStatus.Successful, "4")]
        [TestCase("guid008", 4, 3, true, "guid008", "4", 4, SyncStatus.Timeout, null)]
        [TestCase("guid009", 4, 3, true, "guid009", "4", 5, SyncStatus.Timeout, null)]
        [TestCase("guid010", 4, 0, true, "guid010", "4", 1, SyncStatus.Timeout, null)]
        public void SyncRequestWithoutRequestOutputTestMethod(
            string requestInputSn,
            int requestInputArg,
            int syncTimeout,
            bool requestOutputValue,
            string responseSn,
            string responseValue,
            int responseDelay,
            SyncStatus syncStatus,
            string responseValueSynced
            )
        {
            RequestSynchronizer<RequestInput, RequestOutput, Response, string> synchronizer = new RequestSynchronizer<RequestInput, RequestOutput, Response, string>(
                input => new RequestOutput() { Result = input.Arg % 2 == 0 },
                (input, output) => output.Result,
                input => input.SN,
                response => response.NS);

            RequestInput requestInput = new RequestInput() { SN = requestInputSn, Arg = requestInputArg };

            Task.Run(() =>
            {
                Thread.Sleep(TimeSpan.FromSeconds(responseDelay));
                synchronizer.FeedResponse(new Response() { NS = responseSn, Value = responseValue });
            });

            Thread.Sleep(500);

            var actSyncStatus = synchronizer.SyncRequest(requestInput, out RequestOutput requestOutput, out Response response, TimeSpan.FromSeconds(syncTimeout));

            Assert.IsNotNull(requestOutput);
            Assert.IsTrue(requestOutputValue == requestOutput.Result);
            Assert.IsTrue(actSyncStatus == syncStatus);
            Assert.IsTrue(response?.Value == responseValueSynced);
        }
    }
}