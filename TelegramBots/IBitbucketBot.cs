using System;
using System.Threading.Tasks;

namespace TelegramBots
{
    public interface IBitbucketBot
    {
        Task HandlePushWebhook(PushMessage pushMessage);
    }

    #region PushMessage
    public class PushMessage
    {
        public string actor { get; set; }
        public string repository { get; set; }
        public Push push { get; set; }
    }

    public class Push
    {
        public Change[] changes { get; set; }
    }

    public class Change
    {
        public New _new { get; set; }
        public Old old { get; set; }
        public Links6 links { get; set; }
        public bool created { get; set; }
        public bool forced { get; set; }
        public bool closed { get; set; }
        public Commit[] commits { get; set; }
        public bool truncated { get; set; }
    }

    public class New
    {
        public string type { get; set; }
        public string name { get; set; }
        public Target target { get; set; }
        public Links2 links { get; set; }
    }

    public class Target
    {
        public string type { get; set; }
        public string hash { get; set; }
        public string author { get; set; }
        public string message { get; set; }
        public DateTime date { get; set; }
        public Parent[] parents { get; set; }
        public Links links { get; set; }
    }

    public class Links
    {
        public Self self { get; set; }
        public Html html { get; set; }
    }

    public class Self
    {
        public string href { get; set; }
    }

    public class Html
    {
        public string href { get; set; }
    }

    public class Parent
    {
        public string type { get; set; }
        public string hash { get; set; }
        public Links1 links { get; set; }
    }

    public class Links1
    {
        public Self1 self { get; set; }
        public Html1 html { get; set; }
    }

    public class Self1
    {
        public string href { get; set; }
    }

    public class Html1
    {
        public string href { get; set; }
    }

    public class Links2
    {
        public Self2 self { get; set; }
        public Commits commits { get; set; }
        public Html2 html { get; set; }
    }

    public class Self2
    {
        public string href { get; set; }
    }

    public class Commits
    {
        public string href { get; set; }
    }

    public class Html2
    {
        public string href { get; set; }
    }

    public class Old
    {
        public string type { get; set; }
        public string name { get; set; }
        public Target1 target { get; set; }
        public Links5 links { get; set; }
    }

    public class Target1
    {
        public string type { get; set; }
        public string hash { get; set; }
        public string author { get; set; }
        public string message { get; set; }
        public DateTime date { get; set; }
        public Parent1[] parents { get; set; }
        public Links3 links { get; set; }
    }

    public class Links3
    {
        public Self3 self { get; set; }
        public Html3 html { get; set; }
    }

    public class Self3
    {
        public string href { get; set; }
    }

    public class Html3
    {
        public string href { get; set; }
    }

    public class Parent1
    {
        public string type { get; set; }
        public string hash { get; set; }
        public Links4 links { get; set; }
    }

    public class Links4
    {
        public Self4 self { get; set; }
        public Html4 html { get; set; }
    }

    public class Self4
    {
        public string href { get; set; }
    }

    public class Html4
    {
        public string href { get; set; }
    }

    public class Links5
    {
        public Self5 self { get; set; }
        public Commits1 commits { get; set; }
        public Html5 html { get; set; }
    }

    public class Self5
    {
        public string href { get; set; }
    }

    public class Commits1
    {
        public string href { get; set; }
    }

    public class Html5
    {
        public string href { get; set; }
    }

    public class Links6
    {
        public Html6 html { get; set; }
        public Diff diff { get; set; }
        public Commits2 commits { get; set; }
    }

    public class Html6
    {
        public string href { get; set; }
    }

    public class Diff
    {
        public string href { get; set; }
    }

    public class Commits2
    {
        public string href { get; set; }
    }

    public class Commit
    {
        public string hash { get; set; }
        public string type { get; set; }
        public string message { get; set; }
        public string author { get; set; }
        public Links7 links { get; set; }
    }

    public class Links7
    {
        public Self6 self { get; set; }
        public Html7 html { get; set; }
    }

    public class Self6
    {
        public string href { get; set; }
    }

    public class Html7
    {
        public string href { get; set; }
    }

    #endregion 
}