# Ocelot API Gateway Example

This project demonstrates a simple implementation of the Ocelot API Gateway for .NET-based microservices. The project includes an API gateway (`TestGateway`) and three instances of an API (`TestApi`) running behind it.

## Overview

This example is designed to showcase the basic functionalities of an API gateway. The `TestGateway` routes incoming requests to the running instances of `TestApi`. This allows clients to access multiple services through a single entry point.

## Features

* **API Gateway:** Uses Ocelot to provide a single entry point for incoming requests.
* **Routing:** Forwards incoming requests to downstream services based on rules defined in the `ocelot.json` file.
* **Load Balancing:** Distributes incoming requests among the three instances of `TestApi` using the Round Robin algorithm.
* **Rate Limiting:** Limits the number of requests from a client within a specific time period to prevent abuse.

## Technologies Used

* .NET 9.0
* Ocelot
* Docker

## Getting Started

Follow the steps below to run the project on your local machine:

1.  **Clone the Repository:**
    ```sh
    git clone [https://github.com/profalakbear/ocelot-examples.git](https://github.com/profalakbear/ocelot-examples.git)
    cd ocelot-examples
    ```

2.  **Run Docker Compose:**
    In the root directory of the project, run the following command:
    ```sh
    docker-compose up --build
    ```
    This command will build the Docker images for the three instances of `TestApi` and `TestGateway` and start the containers.

3.  **Test the API:**
    Once the gateway is up and running, you can test it by sending a GET request to the following URL:
    ```
    http://localhost:8080/api/sunucunumarasigetir
    ```
    With each request, you will see the response alternating between `1 Numarali Sunucu`, `2 Numarali Sunucu`, and `3 Numarali Sunucu`. This indicates that the load balancing is working.

    You can also use the following endpoint to list employees:
    ```
    http://localhost:8080/api/calisanlar
    ```

## Project Structure

* **TestApi:** An ASP.NET Core API project that provides simple GET endpoints. It runs in three instances named `api1`, `api2`, and `api3` in the `docker-compose.yml` file.
* **TestGateway:** An ASP.NET Core project configured with Ocelot that routes incoming requests to the `TestApi` services.
* **docker-compose.yml:** The Docker Compose configuration file that defines the `TestApi` and `TestGateway` services and their relationships.
* **TestMyAwesomeSolution.sln:** The Visual Studio solution file containing both projects.

## Configuration

### Ocelot Configuration (`ocelot.json`)

The `ocelot.json` file in the `TestGateway` project defines the behavior of the gateway.

* **Routes:**
    * `UpstreamPathTemplate`: The path of the request the client makes to the gateway.
    * `DownstreamPathTemplate`: The path of the request the gateway makes to the downstream service.
    * `DownstreamHostAndPorts`: The addresses of the downstream services.
    * `LoadBalancerOptions`: Specifies the load balancing algorithm (in this example, "RoundRobin").
    * `RateLimitOptions`: Defines the rate-limiting rules.

### Docker Compose (`docker-compose.yml`)

This file defines the multi-container environment for the project.

* **services:**
    * `api1`, `api2`, `api3`: Three services based on the `TestApi` Docker image. Each is configured with a different `SERVER_NUMBER` environment variable to identify which server is responding.
    * `gateway`: The service that runs the `TestGateway` project and is linked to the `api` services. It exposes port 8080 to the local machine.