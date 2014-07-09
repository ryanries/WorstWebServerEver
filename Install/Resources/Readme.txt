
WORST WEB SERVER EVER (WWSE) - Written by Ryan Ries | myotherpcisacloud.com, 2014
---------------------------------------------------------------------------------
Read this file if you want to know what you are doing.

WWSE can run multiple websites simultaneously. (Or it can run just one.) Each key in the config file corresponds to a website.
Remember that you need to restart the WWSE service for changes made to the config file to take effect.

Example:
	<add key="HTTP1" value="http://+:80/;main;index.html" />

The KEY is the name of the website, and can be whatever you want as long as each website has a unique name.
The VALUE is a list of website parameters, separated by semicolons.

The format is as follows:
    <add key="<website_name>" value="<url_prefix>;<root_directory>;<default_document>;[<certificate_hash>]"

url_prefix:
	Each URL Prefix must meet the following criteria:
	- Must be all lower case.
	- Must start with either http:// or https://
	- Must include a port number.
	- Must end with a / character.
	- You may use wildcards, such as + and *. For example, a URL Prefix of http://+:80/ will tell the service
	to respond to all requests on port 80. If you use a URL Prefix of http://myhost.domain.com:80/ then the service
	will only respond to that exact URL Prefix on port 80. Consider NOT using wildcards if maximum security is a concern.

root_directory
	This is root directory of the website. If a client requests "/", he or she will be asking for the default document
	in the root directory. All root directories for all sites will be located under the "\sites" directory where WWSE
	was installed (Probably C:\Program Files\WorstWebServerEver) So the root of SiteA on the server's file system
	could be C:\Program Files\WorstWebServerEver\sites\SiteA\. If it does not exist the site will not load.

default_document
  This is the document that a requester will receive if he or she requests "/". It is usually something like
  index.html, or default.aspx, or whatever you want it to be.

certificate_hash
  This is only needed if the website uses https. If the site uses http, certificate_hash is ignored. If the
  site uses https but the certificate hash is missing or points to an invalid certificate, that website will
  not run. For best results, your url_prefix will match exactly the FQDN of the subject of the certificate.
  The certificate hash needs to point to a cert that is in the Local Machine Personal (MY) store, is not
  expired, and can be used for Server Authentication. WARNING: Be careful when copying certificiate hashes
  using the Windows cert GUI, it tends to copy an unprintable character at the beginning of the hash which
  will frustrate the hell out of you as you try to figure out what is going on.

Sites may use the same default_document and/or root_directory. This could be useful for example if you wanted
to mirror the same content on both http and https, you would need two different "sites" but they both use
the same content.

You're also free to add or modify MIME types in the mimeTypes.txt file if you want to. This file is scanned
once when the service starts. If a document is served that has no associated MIME type in the file, then
the response simply does not include a content-type header.