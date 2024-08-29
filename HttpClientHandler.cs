using System.Net;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Collections.Generic;
using SafariBooksDownload;


public class CustomHttpClientHandlerInstance 
{
    private static CustomHttpClientHandler customHttpClientHandler;
    private CustomHttpClientHandlerInstance() {
    }

    public static CustomHttpClientHandler Instance
    {
        get
        {
            if (customHttpClientHandler == null)
            {
                customHttpClientHandler = new CustomHttpClientHandler();
            }
            return customHttpClientHandler;
        }
    }


}