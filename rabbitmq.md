# RabbitMQ Setup Using Docker on Windows 11

**Purpose:** Run RabbitMQ using Docker Desktop and access its Management Dashboard.

## Step 1: Ensure Docker Desktop Is Running

1. Open Docker Desktop.
2. Wait until Docker Engine is running.
3. Open PowerShell or Windows Terminal.

## Step 2: Run RabbitMQ Container

Execute the following command:

```powershell
docker run -it --rm --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3.13-management
```

This command automatically downloads the RabbitMQ image and starts the container.

**Understanding the ports:**

| Port | Purpose |
|---|---|
| 5672 | RabbitMQ messaging communication (AMQP). |
| 15672 | RabbitMQ Management Dashboard. |

## Step 3: Access RabbitMQ Dashboard

1. Open your browser.
2. Navigate to:

http://localhost:15672

3. Enter the default credentials:

```text
Username: guest
Password: guest
```

4. Click Login.

You should now see the RabbitMQ Management Dashboard.

## Step 4: Verify RabbitMQ Container

Open another PowerShell terminal and execute:

```powershell
docker ps
```

You should see a container named `rabbitmq` running.

## Step 5: Stop RabbitMQ

Press `Ctrl + C` in the terminal where RabbitMQ is running.

Alternatively, execute:

```powershell
docker stop rabbitmq
```

**Note:** The `--rm` option automatically removes the container when it stops. Any data stored only inside the container will be lost.

## Setup Completed!

RabbitMQ is now running through Docker, and its Management Dashboard is accessible at:

http://localhost:15672

**Official Documentation:**

https://www.rabbitmq.com/docs/download