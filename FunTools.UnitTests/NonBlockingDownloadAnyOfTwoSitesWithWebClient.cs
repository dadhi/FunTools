using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using FluentAssertions;
using NUnit.Framework;

namespace FunTools.UnitTests
{
	[TestFixture]
	public class NonBlockingDownloadAnyOfTwoSitesWithWebClient
	{
		[Test]
		//[Ignore]
		public void I_can_make_NonBlockingDownloadAnyOfTwoSitesWithWebClientTest()
		{
			// Arrange
			// Act
			var result = Await.Any(
				DownloadAsync("http://www.google.com"),
				DownloadAsync("http://www.infoq.com"))
				.WaitResult().Value.Success;

			// Assert
			(result.Contains("google") || result.Contains("infoq")).Should().BeTrue();
		}

		[Test]
		//[Ignore]
		public void If_both_sites_downloads_are_successfull_Then_result_should_contain_first_awaited_download()
		{
			// Arrange
			var errors = new List<Exception>();

			// Act
			var result = Await.AnyOrDefault(
				(x, _) =>
				{
					if (x.IsSuccess)
						return x.Success;

					errors.Add(x.Failure);
					return None.Of<string>();
				},
				null,
				DownloadAsync("http://www.google.com"),
				DownloadAsync("http://www.infoq.com"))
				.WaitResult().Value.Success;

			// Assert
			(result.Contains("google") || result.Contains("infoq")).Should().BeTrue();
		}

		[Test]
		//[Ignore]
		public void If_first_site_download_is_failed_Then_result_should_contain_second_successful_download()
		{
			// Arrange
			var errors = new List<Exception>();

			// Act
			var result = Await.AnyOrDefault(
				(x, _) =>
				{
					if (x.IsSuccess)
						return x.Success;

					errors.Add(x.Failure);
					return None.Of<string>();
				},
				null,
				DownloadAsync("http://דûדû.com"),
				DownloadAsync("http://www.infoq.com"))
				.WaitResult().Value.Success;

			// Assert
			result.Contains("infoq").Should().BeTrue();
		}

		[Test]
		//[Ignore]
		public void If_both_sites_downloads_are_failed_Then_result_should_be_none_and_errors_list_should_contain_two_items()
		{
			// Arrange
			var errors = new List<Exception>();

			// Act
			var result = Await.AnyOrDefault((x, _) =>
				{
					if (x.IsSuccess)
						return x.Success;

					errors.Add(x.Failure);
					return None.Of<string>();
				},
				null,
				DownloadAsync("http://דûדû.com"),
				DownloadAsync("http://ץוץו.com"))
				.WaitResult().Value.Success;

			// Assert
			result.Should().BeNull();
			errors.Should().HaveCount(2);
		}

		#region Implementation

		private static Await<string> DownloadAsync(string uri)
		{
			var url = new Uri(uri);
			return complete =>
			{
				var webClient = new WebClient();
				var awaitDownload = Await.Event<DownloadDataCompletedEventArgs, DownloadDataCompletedEventHandler, string>(
					e =>
					{
						if (e.Error != null)
							e.Error.ReThrow();
						return Encoding.ASCII.GetString(e.Result);
					},
					h => webClient.DownloadDataCompleted += h,
					h => webClient.DownloadDataCompleted -= h,
					a => a.Invoke);

				var download = awaitDownload(complete);

				webClient.DownloadDataAsync(url);

				return download;
			};
		}

		#endregion
	}
}