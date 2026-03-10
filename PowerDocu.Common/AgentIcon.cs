using System;

namespace PowerDocu.Common
{
    public static class AgentIcon
    {
        public static string GetIcon(string type)
        {
            return type switch
            {
                "Trigger" => TriggerIcon,
                "Message" => MessageIcon,
                "CancelAllDialogs" => CancelAllDialogsIcon,
                "SetVariable" => SetVariableIcon,
                "ConditionGroup" => ConditionIcon,
                "LogCustomTelemetry" => LogCustomTelemetryIcon,
                "AdaptiveCard" => AdaptiveCardIcon,
                "AIModel" => AIModelIcon,
                "Question" => QuestionIcon,
                "InvokeFlow" => FlowIcon,
                "InvokeConnector" => ConnectorIcon,
                "EndConversation" => CancelAllDialogsIcon,
                "EndDialog" => CancelAllDialogsIcon,
                "OAuthInput" => SignInIcon,
                "SearchAndSummarize" => SearchIcon,
                "RedirectToTopic" => TriggerIcon,
                "KnowledgeSource" => SearchIcon,
                "ClearAllVariables" => UnknownControlIcon,
                "CSATQuestion" => UnknownControlIcon,
                "HttpRequest" => UnknownControlIcon,
                "SetTextVariable" => UnknownControlIcon,
                "EditTable" => UnknownControlIcon,
                "ReplaceDialog" => UnknownControlIcon,
                "ParseValue" => UnknownControlIcon,
                _ => UnknownControlIcon,
            };
        }

        public static string IconHeader
        {
            get
            {
                return @"<svg version=""1.1"" xmlns=""http://www.w3.org/2000/svg"" xmlns:xlink=""http://www.w3.org/1999/xlink"" width=""20"" height=""20""><g>";
            }
        }

        public static string IconFooter
        {
            get
            {
                return "</g></svg>";
            }
        }

        public static string UnknownControlIcon
        {
            get
            {
                return IconHeader +
                        @"<circle fill=""#FFF"" cx=""7.5"" cy=""7.5"" r=""6.5"" />
                        <path fill=""#444"" d=""M7.5.031c.688 0 1.349.089 1.984.266a7.487 7.487 0 015.219 5.219c.177.636.266 1.297.266 1.984s-.089 1.35-.266 1.984a7.487 7.487 0 01-5.219 5.219c-.636.178-1.297.266-1.984.266s-1.349-.088-1.984-.266a7.447 7.447 0 01-3.297-1.922A7.466 7.466 0 015.516.297 7.353 7.353 0 017.5.031zm0 14c.599 0 1.177-.078 1.734-.234s1.078-.376 1.562-.66a6.576 6.576 0 002.34-2.34 6.537 6.537 0 00.895-3.297 6.4 6.4 0 00-.234-1.734 6.54 6.54 0 00-4.562-4.562C8.678 1.048 8.099.969 7.5.969s-1.177.078-1.734.234-1.078.377-1.563.66a6.576 6.576 0 00-2.34 2.34A6.537 6.537 0 00.969 7.5c0 .6.078 1.178.234 1.734a6.56 6.56 0 001.68 2.882A6.54 6.54 0 007.5 14.031zM7.5 3.5c.344 0 .667.066.969.199s.566.312.793.539.406.491.539.793c.133.302.199.625.199.969 0 .312-.05.585-.148.816-.099.232-.223.442-.372.629s-.308.359-.48.512a9.283 9.283 0 00-.48.461 2.45 2.45 0 00-.372.492A1.185 1.185 0 008 9.5v.5H7v-.5c0-.312.05-.584.148-.816.099-.232.223-.442.372-.629s.308-.358.48-.512c.172-.153.332-.307.48-.461.148-.153.272-.316.371-.488S9 6.224 9 6c0-.208-.039-.403-.117-.586a1.52 1.52 0 00-.32-.477 1.51 1.51 0 00-2.126 0A1.493 1.493 0 006 6H5c0-.344.065-.667.195-.969a2.533 2.533 0 011.332-1.332c.305-.133.629-.199.973-.199zM7 11h1v1H7v-1z"" />" +
                        IconFooter;
            }
        }
        public static string TriggerIcon
        {
            get
            {
                return IconHeader +
                        @"<path d=""M12 8c.38 0 .7.28.74.65l.01.1v10.7l2.22-2.21a.75.75 0 0 1 .98-.08l.08.08c.27.26.3.68.07.97l-.07.09-3.5 3.5a.75.75 0 0 1-.98.07l-.08-.07-3.5-3.5a.75.75 0 0 1 .98-1.14l.08.08 2.22 2.21V8.75c0-.41.34-.75.75-.75Zm0-6a7 7 0 0 1 1.75 13.78v-1.56a5.5 5.5 0 1 0-3.5 0v1.56A7 7 0 0 1 12 2Zm0 2.5a4.5 4.5 0 0 1 1.75 8.65v-1.71a3 3 0 1 0-3.5 0v1.7A4.5 4.5 0 0 1 12 4.5Z"" fill=""#118dff""></path>" +
                        IconFooter;
            }
        }

        public static string MessageIcon
        {
            get
            {
                return IconHeader +
                        @"<path d=""M10 2a8 8 0 1 1-3.61 15.14l-.12-.07-3.65.92a.5.5 0 0 1-.62-.45v-.08l.01-.08.92-3.64-.07-.12a7.95 7.95 0 0 1-.83-2.9l-.02-.37L2 10a8 8 0 0 1 8-8Zm0 1a7 7 0 0 0-6.1 10.42.5.5 0 0 1 .06.28l-.02.1-.75 3.01 3.02-.75a.5.5 0 0 1 .19-.01l.09.02.09.04A7 7 0 1 0 10 3Zm.5 8a.5.5 0 0 1 .09 1H7.5a.5.5 0 0 1-.09-1h3.09Zm2-3a.5.5 0 0 1 .09 1H7.5a.5.5 0 0 1-.09-1h5.09Z"" fill=""#672367""></path>" +
                        IconFooter;
            }
        }


        public static string CancelAllDialogsIcon
        {
            get
            {
                return IconHeader +
                        @"<path d=""M3 5.5A2.5 2.5 0 0 1 5.5 3h7A2.5 2.5 0 0 1 15 5.5v7a2.5 2.5 0 0 1-2.5 2.5h-7A2.5 2.5 0 0 1 3 12.5v-7ZM5.5 4C4.67 4 4 4.67 4 5.5v7c0 .83.67 1.5 1.5 1.5h7c.83 0 1.5-.67 1.5-1.5v-7c0-.83-.67-1.5-1.5-1.5h-7Zm2 13a2.5 2.5 0 0 1-2-1h7a3.5 3.5 0 0 0 3.5-3.5v-7c.6.46 1 1.18 1 2v5a4.5 4.5 0 0 1-4.5 4.5h-5ZM6.85 6.15a.5.5 0 1 0-.7.7L8.29 9l-2.14 2.15a.5.5 0 0 0 .7.7L9 9.71l2.15 2.14a.5.5 0 0 0 .7-.7L9.71 9l2.14-2.15a.5.5 0 0 0-.7-.7L9 8.29 6.85 6.15Z"" fill=""#6bb700""></path>" +
                        IconFooter;
            }
        }

        public static string SetVariableIcon
        {
            get
            {
                return IconHeader +
                        @"<path d=""M5.5 3A2.5 2.5 0 0 0 3 5.5v2.88c0 .5-.28.95-.72 1.17a.5.5 0 0 0 0 .9c.44.22.72.68.72 1.17v2.88A2.5 2.5 0 0 0 5.5 17a.5.5 0 0 0 0-1A1.5 1.5 0 0 1 4 14.5v-2.88c0-.62-.24-1.2-.66-1.62.42-.42.66-1 .66-1.62V5.5C4 4.67 4.67 4 5.5 4a.5.5 0 0 0 0-1Zm9 0A2.5 2.5 0 0 1 17 5.5v2.88c0 .5.28.95.72 1.17a.5.5 0 0 1 0 .9 1.3 1.3 0 0 0-.72 1.17v2.88a2.5 2.5 0 0 1-2.5 2.5.5.5 0 0 1 0-1c.83 0 1.5-.67 1.5-1.5v-2.88c0-.62.24-1.2.66-1.62A2.3 2.3 0 0 1 16 8.38V5.5c0-.83-.67-1.5-1.5-1.5a.5.5 0 0 1 0-1ZM7.9 6.2a.5.5 0 0 0-.8.6L9.38 10l-2.3 3.2a.5.5 0 0 0 .82.6L10 10.85l2.1 2.93a.5.5 0 0 0 .8-.58L10.62 10l2.3-3.2a.5.5 0 1 0-.82-.6L10 9.15 7.9 6.21Z"" fill=""#118dff""></path>" +
                        IconFooter;
            }
        }

        public static string ConditionIcon
        {
            get
            {
                return IconHeader +
                        @"<path d=""M10 3c.28 0 .5.22.5.5V8H13a2 2 0 0 1 2 2v5.3l1.65-1.65a.5.5 0 0 1 .7.7l-2.5 2.5a.5.5 0 0 1-.7 0l-2.5-2.5a.5.5 0 0 1 .7-.7L14 15.29V10a1 1 0 0 0-1-1H7a1 1 0 0 0-1 1v5.3l1.65-1.65a.5.5 0 0 1 .7.7l-2.5 2.5a.5.5 0 0 1-.7 0l-2.5-2.5a.5.5 0 0 1 .7-.7L5 15.29V10c0-1.1.9-2 2-2h2.5V3.5c0-.28.22-.5.5-.5Z"" fill=""#118dff""></path>" +
                        IconFooter;
            }
        }

        public static string LogCustomTelemetryIcon
        {
            get
            {
                return IconHeader +
                        @"<path d=""M6 3a3 3 0 0 0-3 3v8a3 3 0 0 0 3 3h8a3 3 0 0 0 3-3V6a3 3 0 0 0-3-3H6ZM4 6c0-1.1.9-2 2-2h8a2 2 0 0 1 2 2H4Zm0 1h12v7a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V7Z"" fill=""#242424""></path>" +
                        IconFooter;
            }
        }

        public static string FlowIcon
        {
            get
            {
                return IconHeader +
                        @"<path d=""M5 3a2 2 0 0 0-2 2v10a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2V5a2 2 0 0 0-2-2H5Zm2.5 4a1.5 1.5 0 1 1 0 3 1.5 1.5 0 0 1 0-3Zm5 3a1.5 1.5 0 1 1 0 3 1.5 1.5 0 0 1 0-3Zm-5-2a.5.5 0 1 0 0 1 .5.5 0 0 0 0-1Zm5 3a.5.5 0 1 0 0 1 .5.5 0 0 0 0-1ZM9 8.5h2l-1 2H9l1-2Z"" fill=""#0078d4""></path>" +
                        IconFooter;
            }
        }

        public static string ConnectorIcon
        {
            get
            {
                return IconHeader +
                        @"<path d=""M8.5 2a1.5 1.5 0 0 0-1.42 1H5.5A1.5 1.5 0 0 0 4 4.5v4A1.5 1.5 0 0 0 5.5 10h1.58a1.5 1.5 0 0 0 2.84 0h1.58a1.5 1.5 0 0 0 1.5-1.5v-4A1.5 1.5 0 0 0 11.5 3H9.92A1.5 1.5 0 0 0 8.5 2ZM5 8.5v-4a.5.5 0 0 1 .5-.5h6a.5.5 0 0 1 .5.5v4a.5.5 0 0 1-.5.5H5.5a.5.5 0 0 1-.5-.5Zm3.5.5a.5.5 0 1 1 0-1 .5.5 0 0 1 0 1Zm0-6a.5.5 0 1 1 0-1 .5.5 0 0 1 0 1ZM4 12.5A1.5 1.5 0 0 1 5.5 11h6a1.5 1.5 0 0 1 1.5 1.5v2a1.5 1.5 0 0 1-1.5 1.5h-6A1.5 1.5 0 0 1 4 14.5v-2Zm1.5-.5a.5.5 0 0 0-.5.5v2a.5.5 0 0 0 .5.5h6a.5.5 0 0 0 .5-.5v-2a.5.5 0 0 0-.5-.5h-6Z"" fill=""#0078d4""></path>" +
                        IconFooter;
            }
        }

        public static string SignInIcon
        {
            get
            {
                return IconHeader +
                        @"<path d=""M10 2a8 8 0 1 0 0 16 8 8 0 0 0 0-16Zm-7 8a7 7 0 1 1 14 0 7 7 0 0 1-14 0Zm7-3a1.5 1.5 0 1 0 0 3 1.5 1.5 0 0 0 0-3ZM8 8.5a2.5 2.5 0 1 1 4 2 3.5 3.5 0 0 1 2.5 3.36.5.5 0 0 1-1 .03A2.5 2.5 0 0 0 11 11.5H9a2.5 2.5 0 0 0-2.5 2.39.5.5 0 0 1-1-.03A3.5 3.5 0 0 1 8 10.5a2.5 2.5 0 0 1 0-2Z"" fill=""#0078d4""></path>" +
                        IconFooter;
            }
        }

        public static string SearchIcon
        {
            get
            {
                return IconHeader +
                        @"<path d=""M8.5 3a5.5 5.5 0 0 1 4.38 8.82l3.65 3.65a.75.75 0 0 1-.98 1.13l-.08-.07-3.65-3.66A5.5 5.5 0 1 1 8.5 3Zm0 1.5a4 4 0 1 0 0 8 4 4 0 0 0 0-8Z"" fill=""#0078d4""></path>" +
                        IconFooter;
            }
        }

        public static string AIModelIcon
        {
            get
            {
                return IconHeader +
                        @"<path d=""M9.5 2L11.2 7.2L16 8.5L11.2 9.8L9.5 15L7.8 9.8L3 8.5L7.8 7.2Z"" fill=""#672367""></path>
                        <path d=""M15 1.5L15.8 3.5L17.5 4L15.8 4.5L15 6.5L14.2 4.5L12.5 4L14.2 3.5Z"" fill=""#672367""></path>" +
                        IconFooter;
            }
        }

        public static string AdaptiveCardIcon
        {
            get
            {
                return IconHeader +
                        @"<path d=""M3 4.5C3 3.67 3.67 3 4.5 3h11c.83 0 1.5.67 1.5 1.5v11c0 .83-.67 1.5-1.5 1.5h-11C3.67 17 3 16.33 3 15.5v-11ZM4.5 4a.5.5 0 0 0-.5.5v11c0 .28.22.5.5.5h11a.5.5 0 0 0 .5-.5v-11a.5.5 0 0 0-.5-.5h-11ZM6 7h8v1H6V7Zm0 3h8v1H6v-1Zm0 3h5v1H6v-1Z"" fill=""#672367""></path>" +
                        IconFooter;
            }
        }

        public static string QuestionIcon
        {
            get
            {
                return IconHeader +
                        @"<path d=""M10 2a8 8 0 1 1-3.61 15.14l-.12-.07-3.65.92a.5.5 0 0 1-.62-.45v-.08l.01-.08.92-3.64-.07-.12a7.95 7.95 0 0 1-.83-2.9l-.02-.37L2 10a8 8 0 0 1 8-8Zm0 1a7 7 0 0 0-6.1 10.42.5.5 0 0 1 .06.28l-.02.1-.75 3.01 3.02-.75a.5.5 0 0 1 .19-.01l.09.02.09.04A7 7 0 1 0 10 3Z"" fill=""#672367""></path>
                        <text x=""7.5"" y=""13.5"" font-size=""10"" font-weight=""bold"" fill=""#672367"">?</text>" +
                        IconFooter;
            }
        }
    }
}
