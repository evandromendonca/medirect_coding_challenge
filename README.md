# MeDirect

## Currency Converter

### Assumptions

- This is a microservice that will be called by other services, not a front-end facing api
- The client was already authenticated, and the base currency balance verified for the transaction

## Tech Stack

**Database:** PostgreSQL 15
**Caching**: Redis
**Web API:** Net 6
**Logging**: Serilog
**Unit testing**: xUnit, Moq and MockHttp
**Exchange rate providers:**

- Fixer API: https://apilayer.com/marketplace/fixer-api
- Exchange Rates Data API: https://apilayer.com/marketplace/exchangerates_data-api

## How to

- To create the database, execute the sql script `database_script.sql` found on the root of the repository
- For a dockerized version of Redis run `docker run -p 6379:6379 --name redis -d redis`, or update the `appsettings.Development.json` => `ConnectionStrings:Redis` with a connection string to a running instance of Redis
- For the PostgreSql database, the connection string can be found in `appsettings.Development.json` => `ConnectionStrings:CurrencyExchangeDatabase`
- You can find the main project source code in `src\MeDirectCurrencyExchange.Api`

## Structure

- The source code is inside the src folder. The solution is composed of 2 projects and 2 unit test projects

### MeDirectCurrencyExchange.Api

This project is the main web application. There are 2 controllers inside it.
Logs are being generated in the project folder, to change this behavior the `appsettings.json` need to be changed to select another location, and possibly other sinks

**Folder structure**

- Controllers: Controllers of the web api
- Data: EntityFramework context configuration
- DTO: Objects to send and receive data to the outside world
- Exceptions: User defined exceptions
- Extensions: Object extensions
- Middlewares: Middlewares used. The only one is the GlobalExceptionHandler, to deal with exceptions raised by the application
- Models: Domain model
- Repositories: Repository layer, sits on top of the data layer. It is also where the CachedRepository is implemented using the decorator pattern.

#### Rates Controllers

**Endpoint**: [GET] `rates/{baseCurrency}/{targerCurrency}?clientId=0&preferredProvider=fixer`
The user can get a rate for a specific currency pair. It can specify if a client is requesting that rate and the preferred provider for that rate. Currently there are 2 providers options to be used: `exchange_rates_data_api` and `fixer` (default one)

#### Trades Controllers

**Endpoint**: [GET] `trades/{clientId}?from={datetimeoffset}`
Used to get trades from a specific client

**Endpoint**: [POST] `trades`
This is the endpoint used to make trades. It verifies if the rate exists for that client, if it is still valid (not older than 30 minutes) and if the expected value is the actual rate value. It also checks the trading limit (10 trades per hour)
_Body structure_:

```json
{
  "clientId": 0,
  "baseCurrency": "string",
  "targetCurrency": "string",
  "expectedRate": 0,
  "balanceBaseCurrency": 0,
  "fees": 0
}
```

### MeDirectCurrencyExchange.RateProvider

This project is responsible for integrations with currency rate providers. Currently there are 2 providers implemented: _Fixer_ and _Exchange Rates Data API_.
It providers an interface IRateProvider that can be used to implement new providers.
There is a check for currency codes correctness before hitting the apis to avoid using quotas with bad data.

### MeDirectCurrencyExchange.Api.Test

Project with unit tests for the `MeDirectCurrencyExchange.Api` project

### MeDirectCurrencyExchante.RateProvider.Test

Project with unit tests for the `MeDirectCurrencyExchante.RateProvider` project

### Environment variables used by the application

If running locally in development you can use `appsettings.Development.json` to store test connection strings and keys. Otherwise configure the following environment variables
**ConnectionStrings:Redis**: Redis connection string
**ConnectionStrings:CurrencyExchangeDatabase**: PostgreSQL connection string
**RateProviderApiKeys:Fixer**: Api Key for Fixer
**RateProviderApiKeys:ExchangeRatesDataApi**: Api Key for ExchangeRatesDataApi
