#!/bin/bash

helm upgrade --install customer-management-api ./services/customer-management-api/Deployment
helm upgrade --install notification-system-api ./services/notification-system-api/Deployment
helm upgrade --install order-management-api ./services/order-management-api/Deployment
