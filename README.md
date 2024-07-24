# Integración KN y sus Servicios

Este proyecto contiene los servicios necesarios para la integración con KN (Kuehne + Nagel). Arquitectura en microservicios, implementados en .NET Core 8.

## Descripción

El proyecto de integración KN proporciona APIs para manejar la liberación de ondas y la gestión de picking de LPN. Estos servicios permiten la comunicación fluida con los sistemas de KN, garantizando la correcta transmisión de datos entre sistemas.

## Servicios Incluidos

### APIWaveRelease

Este servicio maneja las operaciones relacionadas con la liberación de ondas.

#### Características
- Autenticación JWT.
- Operaciones CRUD para la gestión de ondas.
- Documentación Swagger para facilitar el uso y pruebas de la API.

### APILPNPicking

Este servicio maneja las operaciones relacionadas con el picking de LPN.

#### Características
- Autenticación JWT.
- Gestión de picking de LPN.
- Documentación Swagger para facilitar el uso y pruebas de la API.

