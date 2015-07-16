﻿//
// ConnectionInstrumentTestRunner.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;
using Xamarin.WebTests.ConnectionFramework;
using Xamarin.WebTests.Resources;

namespace Mono.Security.NewTls.TestFramework
{
	public class ConnectionInstrumentTestRunner : InstrumentationTestRunner
	{
		new public ConnectionInstrumentParameters Parameters {
			get { return (ConnectionInstrumentParameters)base.Parameters; }
		}

		public ConnectionInstrumentType Type {
			get { return Parameters.Type; }
		}

		public ConnectionInstrumentTestRunner (IServer server, IClient client, ConnectionInstrumentParameters parameters, MonoConnectionFlags flags)
			: base (server, client, parameters, flags)
		{
		}

		public override Instrumentation CreateInstrument (TestContext ctx)
		{
			var instrumentation = new Instrumentation ();

			var settings = new UserSettings ();

			instrumentation.SettingsInstrument = new ConnectionInstrument (settings, ctx, this);
			instrumentation.EventSink = new ConnectionEventSink (ctx, this);

			if (Parameters.HandshakeInstruments != null)
				instrumentation.HandshakeInstruments.UnionWith (Parameters.HandshakeInstruments);

			return instrumentation;
		}

		public static IEnumerable<ConnectionInstrumentParameters> GetParameters (TestContext ctx, InstrumentationCategory category)
		{
			return GetInstrumentationTypes (ctx, category).Select (t => Create (ctx, category, t));
		}

		public static IEnumerable<ConnectionInstrumentType> GetInstrumentationTypes (TestContext ctx, InstrumentationCategory category)
		{
			switch (category) {
			case InstrumentationCategory.ClientConnection:
				yield return ConnectionInstrumentType.FragmentHandshakeMessages;
				yield return ConnectionInstrumentType.SendBlobAfterReceivingFinish;
				break;

			case InstrumentationCategory.ServerConnection:
				yield return ConnectionInstrumentType.FragmentHandshakeMessages;
				break;

			case InstrumentationCategory.Connection:
				yield return ConnectionInstrumentType.FragmentHandshakeMessages;
				break;

			case InstrumentationCategory.ClientRenegotiation:
				yield return ConnectionInstrumentType.RequestClientRenegotiation;
				break;

			case InstrumentationCategory.ServerRenegotiation:
				yield return ConnectionInstrumentType.RequestRenegotiation;
				yield return ConnectionInstrumentType.SendBlobBeforeHelloRequest;
				yield return ConnectionInstrumentType.SendBlobAfterHelloRequest;
				yield return ConnectionInstrumentType.SendBlobBeforeAndAfterHelloRequest;
				yield return ConnectionInstrumentType.SendDuplicateHelloRequest;
				yield return ConnectionInstrumentType.RequestServerRenegotiation;
				yield return ConnectionInstrumentType.RequestServerRenegotiationWithPendingRead;
				break;

			case InstrumentationCategory.Renegotiation:
				yield return ConnectionInstrumentType.SendBlobBeforeRenegotiatingHello;
				yield return ConnectionInstrumentType.SendBlobBeforeRenegotiatingHelloNoPendingRead;
				break;

			case InstrumentationCategory.MartinTest:
				yield return ConnectionInstrumentType.MartinTest;
				break;

			case InstrumentationCategory.ManualClient:
				yield return ConnectionInstrumentType.MartinClientPuppy;
				break;

			case InstrumentationCategory.ManualServer:
				yield return ConnectionInstrumentType.MartinServerPuppy;
				break;

			default:
				ctx.AssertFail ("Unsupported instrumentation category: '{0}'.", category);
				break;
			}
		}

		static ConnectionInstrumentParameters CreateParameters (InstrumentationCategory category, ConnectionInstrumentType type, params object[] args)
		{
			var sb = new StringBuilder ();
			sb.Append (type);
			foreach (var arg in args) {
				sb.AppendFormat (":{0}", arg);
			}
			var name = sb.ToString ();

			return new ConnectionInstrumentParameters (category, type, name, ResourceManager.SelfSignedServerCertificate) {
				ClientCertificateValidator = AcceptAnyCertificate, ServerCertificateValidator = AcceptAnyCertificate,
				ProtocolVersion = ProtocolVersions.Tls12
			};
		}

		static ConnectionInstrumentParameters Create (TestContext ctx, InstrumentationCategory category, ConnectionInstrumentType type)
		{
			var parameters = CreateParameters (category, type);

			switch (type) {
			case ConnectionInstrumentType.FragmentHandshakeMessages:
				parameters.HandshakeInstruments = new HandshakeInstrumentType[] { HandshakeInstrumentType.FragmentHandshakeMessages };
				break;

			case ConnectionInstrumentType.SendBlobAfterReceivingFinish:
				parameters.HandshakeInstruments = new HandshakeInstrumentType[] { HandshakeInstrumentType.SendBlobAfterReceivingFinish };
				break;

			case ConnectionInstrumentType.RequestRenegotiation:
				parameters.HandshakeInstruments = new HandshakeInstrumentType[] {
					HandshakeInstrumentType.RequestServerRenegotiation
				};
				break;

			case ConnectionInstrumentType.SendBlobBeforeHelloRequest:
				parameters.HandshakeInstruments = new HandshakeInstrumentType[] {
					HandshakeInstrumentType.RequestServerRenegotiation,
					HandshakeInstrumentType.SendBlobBeforeHelloRequest
				};
				break;

			case ConnectionInstrumentType.SendBlobAfterHelloRequest:
				parameters.HandshakeInstruments = new HandshakeInstrumentType[] {
					HandshakeInstrumentType.RequestServerRenegotiation,
					HandshakeInstrumentType.SendBlobAfterHelloRequest
				};
				break;

			case ConnectionInstrumentType.SendBlobBeforeAndAfterHelloRequest:
				parameters.HandshakeInstruments = new HandshakeInstrumentType[] {
					HandshakeInstrumentType.RequestServerRenegotiation,
					HandshakeInstrumentType.SendBlobBeforeHelloRequest,
					HandshakeInstrumentType.SendBlobAfterHelloRequest
				};
				break;

			case ConnectionInstrumentType.SendDuplicateHelloRequest:
				parameters.HandshakeInstruments = new HandshakeInstrumentType[] {
					HandshakeInstrumentType.RequestServerRenegotiation,
					HandshakeInstrumentType.SendDuplicateHelloRequest
				};
				break;

			case ConnectionInstrumentType.RequestServerRenegotiation:
				parameters.RequestServerRenegotiation = true;
				break;

			case ConnectionInstrumentType.RequestServerRenegotiationWithPendingRead:
				parameters.RequestServerRenegotiation = true;
				parameters.QueueServerReadFirst = true;
				break;

			case ConnectionInstrumentType.SendBlobBeforeRenegotiatingHello:
				parameters.RequestServerRenegotiation = true;
				parameters.QueueServerReadFirst = true;
				parameters.HandshakeInstruments = new HandshakeInstrumentType[] {
					HandshakeInstrumentType.SendBlobBeforeRenegotiatingHello
				};
				break;

			case ConnectionInstrumentType.SendBlobBeforeRenegotiatingHelloNoPendingRead:
				parameters.RequestServerRenegotiation = true;
				parameters.HandshakeInstruments = new HandshakeInstrumentType[] {
					HandshakeInstrumentType.SendBlobBeforeRenegotiatingHello
				};
				break;

			case ConnectionInstrumentType.RequestClientRenegotiation:
				parameters.RequestClientRenegotiation = true;
				break;

			case ConnectionInstrumentType.MartinTest:
				parameters.RequestClientRenegotiation = true;
				parameters.HandshakeInstruments = new HandshakeInstrumentType[] {
				};
				parameters.EnableDebugging = true;
				break;

			case ConnectionInstrumentType.MartinClientPuppy:
			case ConnectionInstrumentType.MartinServerPuppy:
				goto case ConnectionInstrumentType.MartinTest;

			default:
				ctx.AssertFail ("Unsupported connection instrument: '{0}'.", type);
				break;
			}

			return parameters;
		}

		public bool HasInstrument (HandshakeInstrumentType type)
		{
			return Parameters.HandshakeInstruments != null && Parameters.HandshakeInstruments.Contains (type);
		}

		protected override Task MainLoop (TestContext ctx, CancellationToken cancellationToken)
		{
			switch (Category) {
			case InstrumentationCategory.ClientRenegotiation:
			case InstrumentationCategory.ServerRenegotiation:
			case InstrumentationCategory.Renegotiation:
			case InstrumentationCategory.MartinTest:
			case InstrumentationCategory.ManualClient:
			case InstrumentationCategory.ManualServer:
				return RunNewMainLoop (ctx, cancellationToken);

			default:
				if (Parameters.Type == ConnectionInstrumentType.SendBlobAfterReceivingFinish)
					return RunMainLoopBlob (ctx, HandshakeInstrumentType.SendBlobAfterReceivingFinish, cancellationToken);
				else
					return base.MainLoop (ctx, cancellationToken);
			}
		}

		async Task RunMainLoopBlob (TestContext ctx, HandshakeInstrumentType type, CancellationToken cancellationToken)
		{
			var expected = Instrumentation.GetTextBuffer (type).GetBuffer ();

			var buffer = new byte [4096];
			int ret = await Server.Stream.ReadAsync (buffer, 0, buffer.Length);
			ctx.Assert (ret, Is.EqualTo (expected.Length));

			buffer = new BufferOffsetSize (buffer, 0, ret).GetBuffer ();

			ctx.Assert (buffer, Is.EqualTo (expected), "blob");

			await Shutdown (ctx, SupportsCleanShutdown, cancellationToken);
		}

		async Task ExpectBlob (TestContext ctx, ICommonConnection connection, HandshakeInstrumentType type, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested ();

			ctx.LogDebug (1, "ExpectBlob: {0} {1}", connection, type);

			var buffer = new byte [4096];
			var blob = Instrumentation.GetTextBuffer (type);
			var ret = await connection.Stream.ReadAsync (buffer, 0, buffer.Length, cancellationToken);

			ctx.LogDebug (1, "ExpectBlob #1: {0} {1} {2}", connection, type, ret);

			ctx.Assert (ret, Is.GreaterThan (0), "read success");
			var result = new BufferOffsetSize (buffer, 0, ret);

			ctx.Expect (result, new IsEqualBlob (blob), "blob");
		}

		async Task HandleClient (TestContext ctx, CancellationToken cancellationToken)
		{
			if ((ConnectionFlags & MonoConnectionFlags.ManualClient) != 0)
				return;

			cancellationToken.ThrowIfCancellationRequested ();

			if (Parameters.RequestClientRenegotiation) {
				ctx.LogDebug (1, "Calling IMonoSslStream.RequestRenegotiation()");
				var monoSslStream = (IMonoSslStream)Client.SslStream;
				await monoSslStream.RequestRenegotiation ();
				ctx.LogDebug (1, "Done calling IMonoSslStream.RequestRenegotiation()");
			}

			ctx.LogDebug (1, "HandleClient");
			if (HasInstrument (HandshakeInstrumentType.SendBlobBeforeHelloRequest))
				await ExpectBlob (ctx, Client, HandshakeInstrumentType.SendBlobBeforeHelloRequest, cancellationToken);

			ctx.LogDebug (1, "HandleClient #1");
			if (HasInstrument (HandshakeInstrumentType.SendBlobAfterHelloRequest))
				await ExpectBlob (ctx, Client, HandshakeInstrumentType.SendBlobAfterHelloRequest, cancellationToken);

			ctx.LogDebug (1, "HandleClient #2");
			await ExpectBlob (ctx, Client, HandshakeInstrumentType.TestCompleted, cancellationToken);

			cancellationToken.ThrowIfCancellationRequested ();

			ctx.LogDebug (1, "HandleClient #3");
			var blob = Instrumentation.GetTextBuffer (HandshakeInstrumentType.TestCompleted);
			await Client.Stream.WriteAsync (blob.Buffer, blob.Offset, blob.Size, cancellationToken);

			ctx.LogDebug (1, "HandleClient #4");
			await Client.Shutdown (ctx, SupportsCleanShutdown, cancellationToken);
			ctx.LogDebug (1, "HandleClient #5");
		}

		async Task HandleServerRead (TestContext ctx, CancellationToken cancellationToken)
		{
			ctx.LogDebug (1, "HandleServerRead");

			if (HasInstrument (HandshakeInstrumentType.SendBlobBeforeRenegotiatingHello))
				await ExpectBlob (ctx, Server, HandshakeInstrumentType.SendBlobBeforeRenegotiatingHello, cancellationToken);

			ctx.LogDebug (1, "HandleServerRead #1");
			await ExpectBlob (ctx, Server, HandshakeInstrumentType.TestCompleted, cancellationToken);

			ctx.LogDebug (1, "HandleServerRead #2");
		}

		async Task HandleServerWrite (TestContext ctx, CancellationToken cancellationToken)
		{
			ctx.LogDebug (1, "HandleServerWrite");

			if (HasInstrument (HandshakeInstrumentType.RequestServerRenegotiation))
				await renegotiationTcs.Task;

			ctx.LogDebug (1, "HandleServerWrite #1");

			var blob = Instrumentation.GetTextBuffer (HandshakeInstrumentType.TestCompleted);
			await Server.Stream.WriteAsync (blob.Buffer, blob.Offset, blob.Size, cancellationToken);

			ctx.LogDebug (1, "HandleServerWrite #2");

			await Server.Shutdown (ctx, SupportsCleanShutdown, cancellationToken);

			ctx.LogDebug (1, "HandleServerWrite #3");
		}

		async Task HandleServer (TestContext ctx, CancellationToken cancellationToken)
		{
			Task readTask = null;

			if (Parameters.QueueServerReadFirst)
				readTask = HandleServerRead (ctx, cancellationToken);

			if (Parameters.RequestServerRenegotiation) {
				ctx.LogDebug (1, "Calling IMonoSslStream.RequestRenegotiation()");
				var monoSslStream = (IMonoSslStream)Server.SslStream;
				await monoSslStream.RequestRenegotiation ();
				ctx.LogDebug (1, "Done calling IMonoSslStream.RequestRenegotiation()");
			}

			if (readTask == null)
				readTask = HandleServerRead (ctx, cancellationToken);

			var writeTask = HandleServerWrite (ctx, cancellationToken);

			await Task.WhenAll (readTask, writeTask);
		}

		async Task RunNewMainLoop (TestContext ctx, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested ();

			var clientTask = HandleClient (ctx, cancellationToken);
			var serverTask = HandleServer (ctx, cancellationToken);

			await Task.WhenAll (clientTask, serverTask);
		}

		TaskCompletionSource<bool> renegotiationTcs;

		public override Task Start (TestContext ctx, CancellationToken cancellationToken)
		{
			renegotiationTcs = new TaskCompletionSource<bool> ();
			return base.Start (ctx, cancellationToken);
		}

		public override Task<bool> Shutdown (TestContext ctx, bool attemptCleanShutdown, CancellationToken cancellationToken)
		{
			renegotiationTcs.TrySetCanceled ();
			return base.Shutdown (ctx, attemptCleanShutdown, cancellationToken);
		}

		void OnRenegotiationCompleted (TestContext ctx)
		{
			ctx.LogMessage ("ON RENEGOTIATION COMPLETED");
			renegotiationTcs.SetResult (true);
		}

		class ConnectionEventSink : InstrumentationEventSink
		{
			public TestContext Context {
				get;
				private set;
			}

			public ConnectionInstrumentTestRunner Runner {
				get;
				private set;
			}

			public ConnectionEventSink (TestContext ctx, ConnectionInstrumentTestRunner runner)
			{
				Context = ctx;
				Runner = runner;
			}

			public void RenegotiationCompleted ()
			{
				Runner.OnRenegotiationCompleted (Context);
			}
		}
	}
}

