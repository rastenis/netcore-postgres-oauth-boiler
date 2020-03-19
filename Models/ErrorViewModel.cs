using System;

namespace netcore_postgres_oauth_boiler.Models
{
	 public class ErrorViewModel
	 {
		  public string RequestId { get; set; }

		  public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
	 }
}
