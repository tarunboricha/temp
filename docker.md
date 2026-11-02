I'll simplify it into a short, beginner-friendly Docker Desktop setup guide for Windows 11, keeping only the installation steps, required configuration and final verification. No Docker theory, advanced commands or unrelated troubleshooting.

# Docker Desktop Setup on Windows 11

**Purpose:** Install and configure Docker Desktop using WSL 2.

## Step 1: Enable WSL 2

1. Open **PowerShell as Administrator**.
2. Execute:

```powershell
wsl --install
```

3. Restart your computer.
4. Open Ubuntu from the Start menu.
5. Create your Linux username and password.

## Step 2: Update WSL

Open PowerShell and execute:

```powershell
wsl --update
```

Verify installation:

```powershell
wsl --version
```

Check the installed Linux distribution:

```powershell
wsl -l -v
```

Ensure Ubuntu uses **Version 2**.

## Step 3: Download Docker Desktop

1. Visit https://www.docker.com/products/docker-desktop/
2. Click **Download Docker Desktop**.
3. Select the Windows installer suitable for your processor.
4. Download the installer.

## Step 4: Install Docker Desktop

1. Run `Docker Desktop Installer.exe`.
2. Follow the installation instructions.
3. Select **Use WSL 2 instead of Hyper-V** if prompted.
4. Complete the installation.
5. Restart your computer if required.

## Step 5: Configure Docker Desktop

1. Open Docker Desktop.
2. Navigate to **Settings → General**.
3. Enable **Use the WSL 2 based engine**.
4. Navigate to **Settings → Resources → WSL Integration**.
5. Enable integration for Ubuntu.
6. Click **Apply & Restart**.

## Step 6: Verify Docker Installation

Open PowerShell or Ubuntu terminal and execute:

**Check Docker version:**

```powershell
docker --version
```

**Check Docker Compose:**

```powershell
docker compose version
```

**Check Docker Engine:**

```powershell
docker info
```

## Step 7: Run Your First Docker Container

Execute:

```powershell
docker run hello-world
```

If Docker is installed successfully, you should see:

```text
Hello from Docker!
This message shows that your installation
appears to be working correctly.
```

<aside>
**Setup completed successfully!**

Docker Desktop is installed and ready to use on Windows 11.
</aside>

## Official Documentation

- Docker Desktop: https://docs.docker.com/desktop/setup/install/windows-install/
- WSL Installation: https://learn.microsoft.com/en-us/windows/wsl/install
