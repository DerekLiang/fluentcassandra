﻿using System;
using System.Linq;
using NUnit.Framework;

namespace FluentCassandra.Connections
{
	[TestFixture]
	public class NormalConnectionProviderTests
	{
		/// <summary>
		/// Needed to switch to testing ports since the network timeout was making the tests unbearably long.
		/// </summary>
		private readonly string FailoverConnectionString = "Keyspace=Testing;Connection Timeout=1;Server=127.0.0.1:1234,127.0.0.1:4567,127.0.0.1";

		[Test]
		public void Fails_Over()
		{
			// arrange
			var expectedHost = "127.0.0.1";
			var expectedPort = Server.DefaultPort;

			// act
			var result = new ConnectionBuilder(FailoverConnectionString);
			var provider = ConnectionProviderFactory.Get(result);
			var conn = provider.Open();
			var actualHost = conn.Server.Host;
			var actualPort = conn.Server.Port;

			// assert
			Assert.AreEqual(expectedHost, actualHost);
			Assert.AreEqual(expectedPort, actualPort);
		}
	}
}
