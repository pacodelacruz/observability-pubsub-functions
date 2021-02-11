# Pub-Sub Observability Sample

This solution demonstrate how to implement custom tracing and some observability practices in Azure Functions adding business-related metadata, leveraging the structured logging capabilities, and going beyond the out-of-the-box features to meet some of the common requirements that operations teams have. 

The suggested approach works well in integration solutions following the publish-subscribe integration pattern implemented using Azure Functions. It also considers the splitter integration pattern. Bear in mind that this approach is opinionated and based on many years of experience with this type of solutions. Similar practices could be used for other types of scenarios.
