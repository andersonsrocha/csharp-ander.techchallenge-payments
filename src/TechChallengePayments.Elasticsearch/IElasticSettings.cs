﻿namespace TechChallengePayments.Elasticsearch;

public interface IElasticSettings
{
    string ApiKey { get; set; }
    string CloudId { get; set; }
}