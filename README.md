# fabric-config-svc
Configuration service for managing key-value pair config settings in a microservice instead of settings files

Based on Microfost Service Fabric stateful services for storing semantically versioned key value pairs with high persistance and availability. Backs up config settings to nosql database (mongo).

Also contains a client package which pulls updates from the fabric service and caches the config settings locally

## TODO
- add support for wildcard matching semvers
- add support for creating more complex config values, such as nested objects
