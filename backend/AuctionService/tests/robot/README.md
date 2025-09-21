# Robot Framework Tests for Auction Service

These tests exercise the Auction Service HTTP API using [Robot Framework](https://robotframework.org/robotframework/latest/RobotFrameworkUserGuide.html) and the RequestsLibrary.

## Installation

1. Ensure Python 3.9+ is available on your PATH.
2. (Optional) Create and activate a virtual environment.
3. Install Robot Framework and dependencies:
   ```bash
   pip install -r requirements.txt
   ```

The packages mirror the Installation instructions from the Robot Framework user guide.

## Test layout

- `auction_service.robot` – top-level suite covering basic API smoke scenarios.
- `resources/Common.resource` – shared session setup/teardown keywords and variables.
- `requirements.txt` – pinned Python packages for repeatable installs.

## Running the tests

1. Start the Auction Service locally (e.g. `dotnet run --project src/AuctionService.Api`).
2. Export `AUCTION_SERVICE_BASE_URL` if the service is not running on `http://localhost:5000`:
   ```bash
   export AUCTION_SERVICE_BASE_URL="http://localhost:5169"
   ```
3. Execute the suite from the `tests/robot` directory (or supply the path):
   ```bash
   robot auction_service.robot
   ```

Robot will create an HTTP session per suite, perform a read-only list call, then create and delete a temporary item to validate mutation endpoints.

Generated output (`output.xml`, `report.html`, `log.html`) will appear in the execution directory by default.
