﻿using Estranged.Automation.Runner.Reviews;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Estranged.Automation
{
    public class RunnerManager
    {
        private readonly ILogger<RunnerManager> logger;
        private readonly ReviewsRunner reviewsRunner;

        public RunnerManager(ILogger<RunnerManager> logger, ReviewsRunner reviewsRunner)
        {
            this.logger = logger;
            this.reviewsRunner = reviewsRunner;
        }

        public async Task Run()
        {
            logger.LogInformation("Gathering reviews for app 582890");
            await reviewsRunner.GatherReviews(582890);
        }
    }
}