using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using NUnit.Framework;

namespace FunTools.UnitTests
{
	[TestFixture]
	public class NonBlockingDownloadAnyOfTwoSitesWithWebClient
	{
		[Test]
		public void I_can_make_NonBlockingDownloadAnyOfTwoSitesWithWebClientTest()
		{
            var urls = new[] { "http://www.codeproject.com/", "http://smellegantcode.wordpress.com/" };

			var success = urls.Select(DownloadAsync)
				.AwaitSome((x, _) => Some.Of(x.Success))
				.WaitSuccess();

            Assert.That(success, Is.StringContaining("codeproject").Or.StringContaining("smell"));
		}

		[Test]
		public void If_both_sites_downloads_are_successfull_Then_result_should_contain_first_awaited_download()
		{
            var urls = new[] { "http://www.codeproject.com/", "http://smellegantcode.wordpress.com/" };
			var errors = new List<Exception>();

			var result = urls.Select(DownloadAsync)
				.AwaitSome(x => x.OnError(errors.Add).ConvertTo(Some.Of, None.Of<string>()))
				.WaitSuccess();

            Assert.That(result, Is.StringContaining("codeproject").Or.StringContaining("smell"));
		}

		[Test]
		public void If_first_site_download_is_failed_Then_result_should_contain_second_successful_download()
		{
			var errors = new List<Exception>();

			var result = Await.Many(
				(x, _) => x.OnError(errors.Add).ConvertTo(Some.Of, None.Of<string>()),
				null,
				DownloadAsync("http://דûדû.com"),
                DownloadAsync("http://www.codeproject.com/"))
				.WaitSuccess();

            Assert.That(result, Is.StringContaining("codeproject"));
		}

		[Test]
		public void If_both_sites_downloads_are_failed_Then_result_should_be_none_and_errors_list_should_contain_two_items()
		{
			var errors = new List<Exception>();

			var result = Await.Many(
				(x, _) => x.OnError(errors.Add).ConvertTo(Some.Of, ex => None.Of<string>()),
				null,
				DownloadAsync("http://דûדû.com"),
				DownloadAsync("http://ץוץו.com"))
				.WaitSuccess();

			Assert.That(result, Is.Null);
            Assert.That(errors.Count, Is.EqualTo(2));
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