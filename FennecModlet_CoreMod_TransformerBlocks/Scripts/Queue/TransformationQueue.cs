using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;

public class TransformationQueue
{
    private List<TransformationJob> queue;
    public static Regex queueExistParse = new Regex(@"#Q#(.*?)#_Q#");
    public static Regex queueEntryParse = new Regex(@"#e#([0-9]+?):(.+?):(.+?)#_e#");

    
    /**
     * Adds a TransformationData object to the queue. Returns true, if the items were added successfully.
     */

    public bool Add(TransformationData tData, out ulong transformTime, out TransformationJob job)
    {
        this.CreateQueueIfUndefined();
        job = null;

        transformTime = TransformationQueue.CalculateTransformationTimeAsWorldTime(tData);
        if (this.QueueAlreadyHas(tData))
        {
            return false;
        }

        job = new TransformationJob(transformTime, tData);
        this.queue.Add(job);
        return true;
    }


    /**
     * Adds an entry to the queue with predefined transformation time. This is a re-entry method, so we don't want to check for
     * duplicate data this time around in the queue.
     */

    public bool Add(ulong time, TransformationData tData, bool inProgress = false)
    {
        this.CreateQueueIfUndefined();
        this.queue.Add(new TransformationJob(time, tData, inProgress));
        return true;
    }


    /**
     * Adds a job object to the queue directly.
     */

    public bool Add(TransformationJob job)
    {
        this.CreateQueueIfUndefined();
        this.queue.Add(job);
        return true;
    }



    /**
     * Gets the transformation time from world time
     */

    private static ulong CalculateTransformationTimeAsWorldTime(TransformationData tData)
    {
        ulong worldTime = GameManager.Instance.World.worldTime;
        return worldTime + (ulong)tData.transformationTime;
    }


    /**
     * Checks for duplicate entries in the queue even if they are at different times.
     */

    public bool QueueAlreadyHas(TransformationData tData)
    {
        if (!this.QueueDefinedAndNotEmpty())
        {
            return false;
        }

        foreach (TransformationJob job in this.queue)
        {
            if (tData.IsSameAs(job.GetTransformationData()))
            {
                return true;
            }
        }

        return false;
    }

    
    /**
     * Removes entry from queue if it's defined.
     */

    public bool RemoveEntry(TransformationJob job)
    {
        return this.queue.Remove(job);
    }

    
    /**
     * Returns a lisr of TransformationData objects whose time exceeds the current world time, i.e. those ready to transform.
     */

    public List<TransformationJob> GetNextTransformations()
    {
        List<TransformationJob> collection = new List<TransformationJob>();
        if (!this.QueueDefinedAndNotEmpty())
        {
            return collection;
        }

        ulong worldTime = GameManager.Instance.World.worldTime;
        foreach (TransformationJob job in this.queue)
        {
            if (job.IsInProgress())
            {
                continue;
            }

            if (job.IsReady(worldTime))
            {
                collection.Add(job);
            }
        }

        return collection;
    }


    /**
     * Shows if a job at position is defined.
     */

    private bool JobIsDefined(int i)
    {
        return (this.QueueHasJobs() && this.queue[i] != null);
    }


    /**
     * Checks that the queue is defined. If not, make one.
     */

    private void CreateQueueIfUndefined()
    {
        if (!this.QueueDefined())
        {
            this.queue = new List<TransformationJob>();
        }
    }


    /**
     * Checkes the queue is defined and it has at least one entry.
     */

    public bool QueueDefinedAndNotEmpty()
    {
        if (!this.QueueDefined())
        {
            return false;
        }
        
        if (!this.QueueHasJobs())
        {
            return false;
        }

        return true;
    }


    /**
     * Check that the queue has at least one job.
     */

    private bool QueueHasJobs()
    {
        if (!this.QueueDefined())
        {
            return false;
        }

        return this.queue.Count > 0;
    }


    /**
     * Checks that the queue is defined.
     */

    private bool QueueDefined()
    {
        return this.queue != null;
    }
    

    /**
     * Reads a queue string into and builds a transformation queue from it.
     */

    public TransformationQueue Read(string _s, bool fromHash = false)
    {
        MatchCollection queueExist = TransformationQueue.queueExistParse.Matches(_s);
        if (queueExist.Count == 0)
        {
            throw new Exception("Cannot read queue string " + _s);
        }

        TransformationQueue tQueue = new TransformationQueue();
        MatchCollection jobExist = TransformationQueue.queueEntryParse.Matches(_s);
        
        if (jobExist.Count == 0)
        {
            Log.Out("No jobs found.");
            return tQueue;
        }
        
        foreach (Match matchJob in jobExist)
        {
            Log.Out("Matched: " + matchJob.ToString());
            tQueue.Add(TransformationJob.Read(matchJob.ToString(), fromHash));
        }

        return tQueue;
    }

    
    /**
     * Writes queue data as a string for streaming.
     */

    public string Write(bool toHash = false)
    {
        if (!this.QueueDefinedAndNotEmpty())
        {
            return "#Q##_Q#";
        }

        StringBuilder sb = new StringBuilder();
        sb.Append("#Q#");
        foreach (TransformationJob job in this.queue)
        {
            sb.Append(job.Write(toHash));
        }
        sb.Append("#_Q#");
        return sb.ToString();
    }
}
