using System;
using System.Text;
using System.Text.RegularExpressions;

public class TransformationJob
{
    private ulong time;
    private TransformationData tData;
    private bool inProgress;
    private string jobHash;
    private string tDataHash;
    
    public TransformationJob(ulong time, TransformationData tData, bool inProgress = false)
    {
        this.time       = time;
        this.tData      = tData;
        this.inProgress = inProgress;
        this.jobHash    = this.GetMD5Hash();
        this.tDataHash  = tData.GetMD5Hash();
    }


    /**
     * Returns the transformation data from the queue.
     */

    public TransformationData GetTransformationData()
    {
        return this.tData;
    }


    /**
     * Gets the time.
     */

    public ulong GetTime()
    {
        return this.time;
    }


    /**
     * Returns true if the job is ready to process.
     */

    public bool IsReady(ulong currentTime)
    {
        return (currentTime >= this.time);
    }


    /**
     * Returns true if the job is currently running.
     */

    public bool IsInProgress()
    {
        return this.inProgress;
    }
    

    /**
     * Marks job as running.
     */

    public void MarkJobInProgress()
    {
        this.inProgress = true;
    }


    /**
     * Marks job as not running
     */

    public void MarkJobNotInProgress()
    {
        this.inProgress = false;
    }


    /**
     * Returns true if this job is the same as one passed in.
     */
    
    public bool IsSameAs(TransformationJob _other)
    {
        return (this.jobHash == _other.jobHash);
    }


    /**
     * Returns the MD5 of this object.
     */

    public string GetMD5Hash()
    {
        return MD5Hash.Calculate(this.WriteForHash());
    }


    /**
     * Reads the data from a string.
     */

    public static TransformationJob Read(string _s, bool fromHash = false)
    {
        Match jobExists = TransformationQueue.queueEntryParse.Match(_s);
        if (!jobExists.Success)
        {
            throw new Exception("Could not read job string " + _s);
        }

        string timeString = jobExists.Groups[1].ToString();
        string dataString = jobExists.Groups[2].ToString();
        string boolString = jobExists.Groups[3].ToString();

        ulong transformTime;
        if (!StringParsers.TryParseUInt64(timeString, out transformTime))
        {
            throw new Exception("Could not parse time string " + timeString);
        }

        bool inProgress;
        if (!StringParsers.TryParseBool(boolString, out inProgress))
        {
            throw new Exception("Could not parse in progress string " + boolString);
        }

        TransformationData tData = TransformationData.Read(dataString, fromHash);
        return new TransformationJob(transformTime, tData, inProgress);
    }



    /**
     * Writes out the job to a string.
     */

    public string Write(bool toHash = false)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("#e#");
        sb.Append(this.time.ToString());
        sb.Append(":");
        sb.Append(this.tData.Write(toHash));
        sb.Append(":");
        sb.Append(this.inProgress.ToString());
        sb.Append("#_e#");
        return sb.ToString();
    }



    /**
     * Writes out the job info for hash data, as we don't want to include whether the job is in progress or not when checking
     * if it is defined in the queue.
     */

    public string WriteForHash()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append(this.time.ToString());
        sb.Append(":");
        sb.Append(this.tData.Write(true));
        return sb.ToString();
    }
}
