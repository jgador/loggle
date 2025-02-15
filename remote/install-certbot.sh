sudo apt update
sudo DEBIAN_FRONTEND=noninteractive apt install -y python3 python3-venv libaugeas0
sudo python3 -m venv /opt/certbot/
sudo /opt/certbot/bin/pip install --upgrade pip
sudo /opt/certbot/bin/pip install certbot
sudo ln -s /opt/certbot/bin/certbot /usr/bin/certbot
sudo certbot certonly --standalone -d kibana.loggle.co -m certbot@loggle.co --agree-tos --no-eff-email --preferred-challenges=http-01
