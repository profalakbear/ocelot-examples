# Makefile for TestMyAwesomeProject

# .NET Solution
SOLUTION = TestMyAwesomeSolution.sln

# Docker Compose
COMPOSE_FILE = docker-compose.yml

# Default target
all: build

# ==============================================================================
# BUILD & CLEAN
# ==============================================================================

.PHONY: build
build:
	@echo "Building all projects in solution: $(SOLUTION)..."
	dotnet build $(SOLUTION) -c Release
	@echo "✅ All projects built successfully."

.PHONY: clean
clean:
	@echo "Cleaning all projects in solution: $(SOLUTION)..."
	dotnet clean $(SOLUTION)
	@echo "✅ Clean complete."

.PHONY: restore
restore:
	@echo "Restoring NuGet packages for solution: $(SOLUTION)..."
	dotnet restore $(SOLUTION)
	@echo "✅ NuGet packages restored."


# ==============================================================================
# DOCKER COMPOSE
# ==============================================================================

.PHONY: up
up:
	@echo "Starting all services with Docker Compose..."
	docker-compose -f $(COMPOSE_FILE) up --build -d
	@echo "✅ Services are up and running in detached mode."

.PHONY: down
down:
	@echo "Stopping and removing all services, volumes, and networks..."
	docker-compose -f $(COMPOSE_FILE) down -v
	@echo "✅ All services stopped and cleaned up."

.PHONY: logs
logs:
	@echo "Following logs for all services..."
	docker-compose -f $(COMPOSE_FILE) logs -f

.PHONY: ps
ps:
	@echo "Listing running services..."
	docker-compose -f $(COMPOSE_FILE) ps

.PHONY: restart
restart: down up

# ==============================================================================
# HELP
# ==============================================================================

.PHONY: help
help:
	@echo "Available commands:"
	@echo "  make all        - Build all projects (default)."
	@echo "  make build      - Build all .NET projects in the solution."
	@echo "  make clean      - Clean all .NET projects."
	@echo "  make restore    - Restore NuGet packages."
	@echo "  ---"
	@echo "  make up         - Start all services using Docker Compose."
	@echo "  make down       - Stop and remove all services and volumes."
	@echo "  make restart    - Restart all services."
	@echo "  make logs       - Follow logs from all running services."
	@echo "  make ps         - List running services."
	@echo "  ---"
	@echo "  make help       - Show this help message."

.DEFAULT_GOAL := help
