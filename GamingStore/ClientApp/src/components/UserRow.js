import React, { Component } from 'react';
import { NavLink } from 'react-router-dom';

export class UserRow extends Component {
    static displayName = UserRow.name;

    constructor(props) {
        super(props);

        this.state = {
            username: sessionStorage.getItem('username'),
            token: sessionStorage.getItem('token')
        };
    }

    async deleteUser(id) {
        await fetch(`../api/users/${id}`, {
            method: 'DELETE',
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${this.state.token}`
            }
        })
            .then(response => {
                if (!response.ok) {
                    return response.text().then(error => { throw new Error(error) });
                }
            })
            .catch(error => alert(error.message));
    }

    render() {
        const userId = this.props.id;
        const username = this.props.username;
        const role = this.props.role;
        const profilePicture = this.props.profilePicture;
        
        return <tr>
            <td className="text-center">
                <img src={ `data:image/png;base64,${profilePicture}` } alt="user-profile-pic" width="75" />
            </td>
            <td>{ userId }</td>
            <td>{ username }</td>
            <td>{ role }</td>
            <td>
            {(this.props.calledFromAdmin || (this.state.username === username)) &&
                <div className="text-center d-flex justify-content-around">
                    <NavLink className="btn btn-sm btn-info text-white" to={{
                        pathname: `/users/edit/${userId}`,
                        state: {
                            username: username,
                            role: role,
                            profilePicture: profilePicture
                        }
                    }}>Edit</NavLink>
                    <button className="btn btn-sm btn-danger" onClick={
                        async () => { 
                            await this.deleteUser(userId); 
                            await this.props.getUsers(); 
                        }
                    }>Delete</button>
                </div>
            }
            </td>
        </tr>;
    }
}