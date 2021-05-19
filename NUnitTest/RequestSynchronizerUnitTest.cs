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
        [TestCase("Guid001", -1, 3, false, "Guid001", "-1", 1, SyncStatus.Failed, null)]
        [TestCase("Guid002", -4, 3, true, "Guid002", "-4", 1, SyncStatus.Successful, "-4")]
        [TestCase("Guid003", 0, 3, true, "Guid003", "0", 1, SyncStatus.Successful, "0")]
        [TestCase("Guid004", 1, 3, false, "Guid004", "1", 1, SyncStatus.Failed, null)]
        [TestCase("Guid005", 4, 3, true, "Guid005", "4", 1, SyncStatus.Successful, "4")]
        [TestCase("Guid006", 4, 3, true, "Guid006", "4", 0, SyncStatus.Successful, "4")]
        [TestCase("Guid007", 4, 3, true, "Guid007", "4", 2, SyncStatus.Successful, "4")]
        [TestCase("Guid008", 4, 3, true, "Guid008", "4", 4, SyncStatus.Timeout, null)]
        [TestCase("Guid009", 4, 3, true, "Guid009", "4", 5, SyncStatus.Timeout, null)]
        [TestCase("Guid010", 4, 0, true, "Guid010", "4", 1, SyncStatus.Timeout, null)]
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
        [TestCase("Guid001", -1, 3, false, "Guid001", "-1", 1, SyncStatus.Failed, null)]
        [TestCase("Guid002", -4, 3, true, "Guid002", "-4", 1, SyncStatus.Successful, "-4")]
        [TestCase("Guid003", 0, 3, true, "Guid003", "0", 1, SyncStatus.Successful, "0")]
        [TestCase("Guid004", 1, 3, false, "Guid004", "1", 1, SyncStatus.Failed, null)]
        [TestCase("Guid005", 4, 3, true, "Guid005", "4", 1, SyncStatus.Successful, "4")]
        [TestCase("Guid006", 4, 3, true, "Guid006", "4", 0, SyncStatus.Timeout, null)]
        [TestCase("Guid007", 4, 3, true, "Guid007", "4", 2, SyncStatus.Successful, "4")]
        [TestCase("Guid008", 4, 3, true, "Guid008", "4", 4, SyncStatus.Timeout, null)]
        [TestCase("Guid009", 4, 3, true, "Guid009", "4", 5, SyncStatus.Timeout, null)]
        [TestCase("Guid010", 4, 0, true, "Guid010", "4", 1, SyncStatus.Timeout, null)]
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