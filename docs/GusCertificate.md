# **GusCertificate**

Definition:_+public static class GusCertificate+_

This class has only an static method which will create a self-signed X509 certificate.

## Public methods

_+public static X509Certificate2 CreateSelfSignedCertificate(string SubjectName)+_

_Parameters_

* SubjectName: The name of the subject in the certificate.

This method issues a self-signed X509 certificate using the specified subject. If used for a Web Server, the the subject should be the domain name for which it's valid. But as it is self-signed and the Browsers will warn the user, anything will work ;)
