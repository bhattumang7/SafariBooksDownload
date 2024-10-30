using System.Net;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Collections.Generic;
using SafariBooksDownload;


public class CustomHttpClientHandlerInstance 
{
    private static ORiellyHttpClientAdapter _oRiellyHttpClientAdapter;
    private CustomHttpClientHandlerInstance() {
    }

    public static ORiellyHttpClientAdapter Instance
    {
        get
        {
            if (_oRiellyHttpClientAdapter == null)
            {
                _oRiellyHttpClientAdapter = new ORiellyHttpClientAdapter();
            }
            return _oRiellyHttpClientAdapter;
        }
    }


}