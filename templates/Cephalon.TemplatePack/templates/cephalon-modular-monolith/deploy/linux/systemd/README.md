# Linux systemd deployment

These assets provide the self-hosted Linux baseline for `CephalonTemplateApp` after the host is published through `CephalonFolder.pubxml`.

The generated service files assume:

- published output lives at `/opt/CephalonTemplateApp/current`
- the optional environment override file lives at `/etc/cephalon/CephalonTemplateApp.env`

Files in this folder:

- `CephalonTemplateApp.service`
- `CephalonTemplateApp.env`

From the generated app root, publish the host with:

```powershell
dotnet publish CephalonTemplateApp.csproj -p:PublishProfile=CephalonFolder
```

On the Linux target, install the published output and service assets with:

```bash
sudo install -d /opt/CephalonTemplateApp/current
sudo cp -R ./artifacts/publish/CephalonTemplateApp/. /opt/CephalonTemplateApp/current/
sudo install -d /etc/cephalon
sudo install -m 0644 ./deploy/linux/systemd/CephalonTemplateApp.env /etc/cephalon/CephalonTemplateApp.env
sudo install -m 0644 ./deploy/linux/systemd/CephalonTemplateApp.service /etc/systemd/system/CephalonTemplateApp.service
sudo systemd-analyze verify /etc/systemd/system/CephalonTemplateApp.service
sudo systemctl daemon-reload
sudo systemctl enable --now CephalonTemplateApp.service
```

To inspect startup logs:

```bash
sudo journalctl -u CephalonTemplateApp.service -f
```

Then inspect the running host with `/engine`, `/engine/snapshot`, `/health/ready`, and `/scalar`.
