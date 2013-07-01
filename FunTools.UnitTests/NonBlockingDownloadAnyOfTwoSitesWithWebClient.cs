using System;
using System.Collections.Generic;
using System.Linq;
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
			var urls = new[] { "http://www.google.com", "http://www.infoq.com" };

			var success = urls.Select(DownloadAsync)
				.AwaitSome((x, _) => Some.Of(x.Success))
				.WaitSuccess();

			// Assert
			(success.Contains("google") || success.Contains("infoq")).Should().BeTrue();
		}

		[Test]
		//[Ignore]
		public void If_both_sites_downloads_are_successfull_Then_result_should_contain_first_awaited_download()
		{
			// Arrange
			var urls = new[] { "http://www.google.com", "http://www.infoq.com" };
			var errors = new List<Exception>();


			// Act
			var result = urls.Select(DownloadAsync)
				.AwaitSome(x => x.OnFailure(errors.Add).ConvertTo(Some.Of, None.Of<string>()))
				.WaitSuccess();

			// Assert
			(result.Contains("google") || result.Contains("infoq")).Should().BeTrue();
		}

		[Test]
		//[Ignore]
		public void If_first_site_download_is_failed_Then_result_should_contain_second_successful_download()
		{
			// Arrange
			var errors = new List<Exception>();

			// Act;
			var result = Await.Many(
				(x, _) => x.OnFailure(errors.Add).ConvertTo(Some.Of, None.Of<string>()),
				null,
				DownloadAsync("http://דûדû.com"),
				DownloadAsync("http://www.infoq.com"))
				.WaitSuccess();

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
			var result = Await.Many(
				(x, _) => x.OnFailure(errors.Add).ConvertTo(Some.Of, ex => None.Of<string>()),
				null,
				DownloadAsync("http://דûדû.com"),
				DownloadAsync("http://ץוץו.com"))
				.WaitSuccess();

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
					e => Result.Of(Encoding.ASCII.GetString(e.Result), e.Error).Success,
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