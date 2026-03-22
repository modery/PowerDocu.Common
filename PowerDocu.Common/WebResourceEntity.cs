namespace PowerDocu.Common
{
    public class WebResourceEntity
    {
        public string Id;
        public string Name;
        public string DisplayName;
        public string WebResourceType;
        public string IntroducedVersion;
        public bool IsCustomizable;
        public bool IsHidden;
        public string FileName;
        public byte[] Content;

        public string GetTypeDisplayName()
        {
            return WebResourceType switch
            {
                "1" => "HTML",
                "2" => "CSS",
                "3" => "JScript",
                "4" => "XML",
                "5" => "PNG",
                "6" => "JPG",
                "7" => "GIF",
                "8" => "Silverlight (XAP)",
                "9" => "Stylesheet (XSL)",
                "10" => "ICO",
                "11" => "SVG",
                "12" => "RESX",
                _ => WebResourceType ?? "Unknown"
            };
        }

        public bool IsImageType()
        {
            return WebResourceType is "5" or "6" or "7" or "10" or "11";
        }

        public bool IsTextType()
        {
            return WebResourceType is "1" or "2" or "3" or "4" or "9" or "11" or "12";
        }

        public string GetFileExtension()
        {
            return WebResourceType switch
            {
                "1" => ".html",
                "2" => ".css",
                "3" => ".js",
                "4" => ".xml",
                "5" => ".png",
                "6" => ".jpg",
                "7" => ".gif",
                "8" => ".xap",
                "9" => ".xsl",
                "10" => ".ico",
                "11" => ".svg",
                "12" => ".resx",
                _ => ""
            };
        }

        public string GetMimeType()
        {
            return WebResourceType switch
            {
                "1" => "text/html",
                "2" => "text/css",
                "3" => "application/javascript",
                "4" => "text/xml",
                "5" => "image/png",
                "6" => "image/jpeg",
                "7" => "image/gif",
                "10" => "image/x-icon",
                "11" => "image/svg+xml",
                _ => "application/octet-stream"
            };
        }
    }
}
